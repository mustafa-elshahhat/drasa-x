"""
make_seed_pdf.py - generate a small, DETERMINISTIC local-development PDF for the
RAG index. Pure standard-library (no extra dependency): it emits a minimal but
valid PDF whose text is extractable by pypdf/PyPDFLoader.

The content is a short, self-authored science note (water cycle + states of
matter) used ONLY as local seed curriculum so a genuine RAG retrieval can run
offline-deterministically. It is not copyrighted third-party material.

Usage:  python scripts/make_seed_pdf.py
Output: data/pdfs/Grade_8/science.pdf   (subject="science", grade=8)
"""
from pathlib import Path

LINES = [
    "Grade 8 Science - Local Development Seed Notes",
    "",
    "The Water Cycle",
    "The water cycle is the continuous movement of water on Earth.",
    "Evaporation turns liquid water into water vapour using heat from the Sun.",
    "Condensation turns water vapour back into tiny liquid droplets, forming clouds.",
    "Precipitation is water that falls from clouds as rain, snow, or hail.",
    "Collection is when fallen water gathers in oceans, lakes, and rivers.",
    "",
    "States of Matter",
    "Matter exists in three common states: solid, liquid, and gas.",
    "A solid has a fixed shape and a fixed volume.",
    "A liquid has a fixed volume but takes the shape of its container.",
    "A gas has neither a fixed shape nor a fixed volume and fills its container.",
    "Melting is the change from solid to liquid; freezing is liquid to solid.",
    "Boiling is the rapid change from liquid to gas at the boiling point.",
]


def pdf_escape(s: str) -> str:
    return s.replace("\\", r"\\").replace("(", r"\(").replace(")", r"\)")


def build_pdf() -> bytes:
    # Build a single-page content stream: one line per Td step.
    parts = ["BT", "/F1 12 Tf", "50 760 Td", "14 TL"]
    first = True
    for line in LINES:
        if first:
            parts.append(f"({pdf_escape(line)}) Tj")
            first = False
        else:
            parts.append("T*")
            parts.append(f"({pdf_escape(line)}) Tj")
    parts.append("ET")
    content = ("\n".join(parts)).encode("latin-1")

    objects = []
    objects.append(b"<< /Type /Catalog /Pages 2 0 R >>")
    objects.append(b"<< /Type /Pages /Kids [3 0 R] /Count 1 >>")
    objects.append(
        b"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] "
        b"/Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >>"
    )
    objects.append(
        b"<< /Length " + str(len(content)).encode() + b" >>\nstream\n" + content + b"\nendstream"
    )
    objects.append(b"<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>")

    out = bytearray(b"%PDF-1.4\n")
    offsets = []
    for i, body in enumerate(objects, start=1):
        offsets.append(len(out))
        out += f"{i} 0 obj\n".encode() + body + b"\nendobj\n"

    xref_pos = len(out)
    n = len(objects) + 1
    out += f"xref\n0 {n}\n".encode()
    out += b"0000000000 65535 f \n"
    for off in offsets:
        out += f"{off:010d} 00000 n \n".encode()
    out += b"trailer\n"
    out += f"<< /Size {n} /Root 1 0 R >>\n".encode()
    out += b"startxref\n"
    out += f"{xref_pos}\n".encode()
    out += b"%%EOF"
    return bytes(out)


def main():
    base = Path(__file__).resolve().parent.parent
    out_dir = base / "data" / "pdfs" / "Grade_8"
    out_dir.mkdir(parents=True, exist_ok=True)
    out_file = out_dir / "science.pdf"
    out_file.write_bytes(build_pdf())
    print(f"Wrote {out_file} ({out_file.stat().st_size} bytes)")


if __name__ == "__main__":
    main()
