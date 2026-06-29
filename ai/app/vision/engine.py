"""
CV inference engines (Phase 15).

Two engines implement the same small protocol:

  * ``StubVisionEngine`` — deterministic, dependency-free TEST/DEGRADED backend.
    It runs when the heavy CV stack or model files are unavailable (the default
    local/test state). It is CLEARLY LABELLED (``engine="stub"`` + the response
    ``degraded=true`` flag + readiness reports CV not-ready) so its output is
    NEVER presented as a real model's certainty. It produces deterministic
    pseudo-detections derived from the image content hash so backend/UI flows
    (candidate creation, review, sequence engagement NotReady->Ready) can be
    exercised and asserted without faking real recognition or random results.

  * ``TorchVisionEngine`` — the PRODUCTION engine adapting the reference repo
    (RetinaFace/MTCNN detection -> FaceNet embedding -> cosine/centroid
    recognition with a threshold (-> Unknown) -> ResNet emotion -> CNN-BiLSTM
    16-frame engagement). ALL heavy imports are lazy (inside methods) so this
    module imports with zero CV deps; ``available()`` reflects whether deps AND
    local model files are present. It NEVER downloads weights.

Neither engine maps a face to a DerasaX student. Recognition returns only an
OPAQUE external label id + confidence; the backend owns identity mapping via
tenant-scoped enrollment records (or leaves the face Unknown / review-required).
"""
from __future__ import annotations

import os
from dataclasses import dataclass, field
from pathlib import Path
from typing import Any, Dict, List, Optional, Protocol, Tuple

from . import EMOTION_LABELS, ENGAGEMENT_LABELS
from .preprocess import image_signature


@dataclass
class RawFace:
    """Per-face inference output BEFORE backend identity mapping."""
    bbox: List[int]                       # [x1, y1, x2, y2] geometry only (no pixels)
    track_key: str                        # stable-within-session id used for buffering
    external_label: str                   # always "Unknown" unless an enrolled label map exists
    external_label_id: Optional[str]      # OPAQUE cluster/track id — NOT a DerasaX student id
    recognition_confidence: float
    emotion: str
    emotion_confidence: float
    engagement_token: Any                 # ephemeral per-frame feature for the sequence model
    quality_flags: List[str] = field(default_factory=list)


class VisionEngine(Protocol):
    name: str
    model_version: str

    def available(self) -> bool: ...
    def model_versions(self) -> Dict[str, str]: ...
    def analyze_frame(self, image: Any, raw_bytes: bytes, threshold: float) -> List[RawFace]: ...
    def predict_engagement(self, tokens: List[Any]) -> Tuple[str, float]: ...


# ---------------------------------------------------------------------------
# STUB — deterministic, no heavy deps. Test/degraded backend only.
# ---------------------------------------------------------------------------
class StubVisionEngine:
    name = "stub"
    model_version = "stub-cv-2026.06"

    def available(self) -> bool:
        return True

    def model_versions(self) -> Dict[str, str]:
        return {
            "detector": "stub",
            "recognition": "stub",
            "emotion": "stub",
            "engagement": "stub",
        }

    def analyze_frame(self, image: Any, raw_bytes: bytes, threshold: float) -> List[RawFace]:
        sig = image_signature(raw_bytes)
        # Deterministic 1..3 faces per distinct image.
        n = (int(sig[0:2], 16) % 3) + 1
        try:
            width, height = int(image.width), int(image.height)
        except Exception:
            width, height = 640, 480
        faces: List[RawFace] = []
        for i in range(n):
            seg = (sig[i * 10:i * 10 + 10] or sig[:10]).ljust(10, "0")
            conf = (int(seg[0:3], 16) % 1000) / 1000.0
            emo = EMOTION_LABELS[int(seg[3], 16) % len(EMOTION_LABELS)]
            emo_conf = 0.50 + (int(seg[4], 16) % 50) / 100.0  # 0.50..0.99 deterministic
            token = (int(seg[5:7], 16) % 256) / 255.0          # 0..1 deterministic engagement feature
            x1 = (int(seg[7], 16) * width) // 16
            y1 = (int(seg[8], 16) * height) // 16
            bw = max(16, width // 6)
            bh = max(16, height // 6)
            bbox = [x1, y1, min(x1 + bw, width), min(y1 + bh, height)]
            ext_id = "ext-" + sig[i * 6:i * 6 + 10]
            faces.append(
                RawFace(
                    bbox=bbox,
                    track_key=ext_id,                 # stable per face appearance within a session
                    external_label="Unknown",         # the stub never invents a human/student name
                    external_label_id=ext_id,          # opaque, synthetic cluster id
                    recognition_confidence=round(conf, 4),
                    emotion=emo,
                    emotion_confidence=round(emo_conf, 4),
                    engagement_token=token,
                    quality_flags=["stub_engine"],
                )
            )
        return faces

    def predict_engagement(self, tokens: List[Any]) -> Tuple[str, float]:
        if not tokens:
            return (ENGAGEMENT_LABELS[1], 0.5)
        avg = sum(float(t) for t in tokens) / len(tokens)
        label = ENGAGEMENT_LABELS[0] if avg >= 0.5 else ENGAGEMENT_LABELS[1]
        confidence = min(1.0, round(0.5 + abs(avg - 0.5), 4))
        return (label, confidence)


# ---------------------------------------------------------------------------
# TORCH — production engine (lazy, local-only, no downloads). Imports clean with
# zero CV deps; available() is False until deps + model files are present.
# ---------------------------------------------------------------------------
class TorchVisionEngine:
    name = "torch"
    model_version = os.environ.get("CV_MODEL_VERSION", "cv-2026.06")

    # Model filenames expected inside CV_MODEL_DIR (no network downloads).
    EMOTION_FILE = os.environ.get("CV_EMOTION_FILENAME", "emotion.pth")
    ENGAGEMENT_FILE = os.environ.get("CV_ENGAGEMENT_FILENAME", "engagement.pth")
    SVM_FILE = os.environ.get("CV_RECOGNITION_SVM_FILENAME", "svm_face.pkl")
    NORM_FILE = os.environ.get("CV_RECOGNITION_NORM_FILENAME", "normalizer.pkl")
    CENTROIDS_FILE = os.environ.get("CV_RECOGNITION_CENTROIDS_FILENAME", "centroids.pkl")

    def __init__(self, model_dir: Path):
        self._dir = Path(model_dir)
        self._loaded = False
        self._device = None
        self._emotion_model = None
        self._engagement_model = None
        self._svm = None
        self._normalizer = None
        self._centroids = None
        self._facenet = None

    # -- capability checks ---------------------------------------------------
    @staticmethod
    def _deps_present() -> bool:
        # Use find_spec so a readiness check NEVER triggers a heavy torch import.
        import importlib.util
        return all(
            importlib.util.find_spec(m) is not None
            for m in ("torch", "torchvision", "cv2", "numpy")
        )

    def _emotion_engagement_present(self) -> bool:
        return (self._dir / self.EMOTION_FILE).exists() and (self._dir / self.ENGAGEMENT_FILE).exists()

    def recognition_present(self) -> bool:
        return (
            (self._dir / self.SVM_FILE).exists()
            and (self._dir / self.NORM_FILE).exists()
            and (self._dir / self.CENTROIDS_FILE).exists()
        )

    def available(self) -> bool:
        # The detection+emotion+engagement models are the minimum to be "ready";
        # recognition is optional/enrollment-backed (Unknown when absent).
        return self._deps_present() and self._emotion_engagement_present()

    def model_versions(self) -> Dict[str, str]:
        return {
            "detector": "retinaface" if self._deps_present() else "absent",
            "recognition": "facenet+svm" if self.recognition_present() else "absent",
            "emotion": "resnet50-emotion" if (self._dir / self.EMOTION_FILE).exists() else "absent",
            "engagement": "resnet50-bilstm" if (self._dir / self.ENGAGEMENT_FILE).exists() else "absent",
            "model_version": self.model_version,
        }

    # -- lazy model loading (NEVER downloads) --------------------------------
    def _ensure_loaded(self) -> None:
        if self._loaded:
            return
        if not self.available():
            raise RuntimeError("torch vision engine is not available (deps or model files missing)")
        import torch
        from torchvision.models import resnet50

        self._device = torch.device("cuda" if torch.cuda.is_available() else "cpu")

        # Emotion: ResNet50 with a 5-class head (reference emotion_service.py).
        import torch.nn as nn
        emo = resnet50(weights=None)
        emo.fc = nn.Sequential(nn.Linear(2048, 1000), nn.ReLU(), nn.Dropout(0.5), nn.Linear(1000, len(EMOTION_LABELS)))
        emo.load_state_dict(torch.load(self._dir / self.EMOTION_FILE, map_location=self._device))
        emo.to(self._device).eval()
        self._emotion_model = emo

        # Engagement: ResNet50 + BiLSTM (reference engagement_service.py).
        self._engagement_model = _build_engagement_model(self._dir / self.ENGAGEMENT_FILE, self._device)

        # Recognition (optional): FaceNet + SVM + centroids.
        if self.recognition_present():
            import joblib
            from facenet_pytorch import InceptionResnetV1
            self._svm = joblib.load(self._dir / self.SVM_FILE)
            self._normalizer = joblib.load(self._dir / self.NORM_FILE)
            self._centroids = joblib.load(self._dir / self.CENTROIDS_FILE)
            self._facenet = InceptionResnetV1(pretrained="vggface2").eval().to(self._device)

        self._loaded = True

    def _detect(self, image, raw_bytes):
        """RetinaFace detection -> 160x160 CHW tensors + bboxes (reference detector.py)."""
        import cv2
        import numpy as np
        import torch
        from retinaface import RetinaFace

        img = np.array(image)
        faces = RetinaFace.detect_faces(img)
        crops, boxes = [], []
        if isinstance(faces, dict):
            for k in faces:
                x1, y1, x2, y2 = faces[k]["facial_area"]
                crop = img[y1:y2, x1:x2]
                if crop.size == 0:
                    continue
                crop = cv2.resize(crop, (160, 160))
                crop = torch.from_numpy(crop).float().permute(2, 0, 1) / 255.0
                crops.append(crop)
                boxes.append([int(x1), int(y1), int(x2), int(y2)])
        return crops, boxes

    def analyze_frame(self, image: Any, raw_bytes: bytes, threshold: float) -> List[RawFace]:
        self._ensure_loaded()
        import torch
        import torch.nn.functional as F
        from torchvision import transforms

        crops, boxes = self._detect(image, raw_bytes)
        results: List[RawFace] = []
        emo_tf = transforms.Compose([
            transforms.Resize((224, 224)),
            transforms.Normalize(mean=[0.485, 0.456, 0.406], std=[0.229, 0.224, 0.225]),
        ])
        for i, crop in enumerate(crops):
            emo_in = emo_tf(crop).unsqueeze(0).to(self._device)
            with torch.inference_mode():
                logits = self._emotion_model(emo_in)
                probs = F.softmax(logits, dim=1)
                emo_conf, emo_idx = torch.max(probs, dim=1)
            ext_label, ext_id, rec_conf = self._recognize(crop, threshold)
            results.append(
                RawFace(
                    bbox=boxes[i],
                    track_key=ext_id or f"anon-{i}",
                    external_label=ext_label,
                    external_label_id=ext_id,
                    recognition_confidence=round(float(rec_conf), 4),
                    emotion=EMOTION_LABELS[int(emo_idx.item())],
                    emotion_confidence=round(float(emo_conf.item()), 4),
                    engagement_token=emo_in.squeeze(0).detach().cpu(),
                    quality_flags=[],
                )
            )
        return results

    def _recognize(self, crop, threshold: float):
        """FaceNet embedding -> normalize -> SVM -> centroid cosine -> threshold.
        Returns (external_label, external_label_id, confidence). external_label
        stays 'Unknown' (the AI never names a DerasaX student)."""
        if self._facenet is None:
            return ("Unknown", None, 0.0)
        import numpy as np
        import torch
        from sklearn.metrics.pairwise import cosine_similarity

        with torch.inference_mode():
            emb = self._facenet(crop.unsqueeze(0).to(self._device)).squeeze(0).cpu().numpy()
        emb = self._normalizer.transform([emb])
        probs = self._svm.predict_proba(emb)[0]
        pred = int(np.argmax(probs))
        centroid = self._centroids.get(pred) if hasattr(self._centroids, "get") else None
        cosine = 0.0
        if centroid is not None:
            cosine = float(cosine_similarity(emb, np.array(centroid).reshape(1, -1))[0][0])
        if cosine >= threshold:
            # Opaque cluster id only; identity mapping is the backend's job.
            return ("Unknown", f"cls-{pred}", cosine)
        return ("Unknown", None, cosine)

    def predict_engagement(self, tokens: List[Any]) -> Tuple[str, float]:
        self._ensure_loaded()
        import torch
        import torch.nn.functional as F

        frames = torch.stack([t for t in tokens]).unsqueeze(0).to(self._device)
        with torch.inference_mode():
            logits = self._engagement_model(frames)
            probs = F.softmax(logits, dim=1)
            conf, pred = torch.max(probs, dim=1)
        return (ENGAGEMENT_LABELS[int(pred.item())], round(float(conf.item()), 4))


def _build_engagement_model(weights_path: Path, device):
    """CNN(ResNet50)+BiLSTM engagement model (reference engagement_service.py)."""
    import torch
    import torch.nn as nn
    from torchvision import models

    class CNN_BiLSTM(nn.Module):
        def __init__(self):
            super().__init__()
            self.cnn = models.resnet50(weights=None)
            self.cnn.fc = nn.Identity()
            self.lstm = nn.LSTM(2048, 256, num_layers=2, batch_first=True, bidirectional=True, dropout=0.3)
            self.dropout = nn.Dropout(0.5)
            self.fc = nn.Linear(512, len(ENGAGEMENT_LABELS))

        def forward(self, x):
            b, t, c, h, w = x.shape
            x = x.view(b * t, c, h, w)
            x = self.cnn(x).view(b, t, 2048)
            _, (h_n, _) = self.lstm(x)
            x = torch.cat([h_n[-2], h_n[-1]], dim=1)
            return self.fc(self.dropout(x))

    model = CNN_BiLSTM()
    ckpt = torch.load(weights_path, map_location=device)
    model.load_state_dict(ckpt["model_state_dict"] if isinstance(ckpt, dict) and "model_state_dict" in ckpt else ckpt)
    return model.to(device).eval()
