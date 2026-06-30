// Quiz answer option as a selectable radio card matching the prototype. Works
// uncontrolled inside a <form> (the quiz attempt reads answers via
// answerPayloadFromForm), so it supports `defaultChecked`; selected styling is
// driven by CSS off the checked input.
export function OptionCard({ name, value, label, marker, defaultChecked, checked, onChange, disabled }) {
  return (
    <label className={`quiz-option${disabled ? ' is-disabled' : ''}`}>
      <input
        type="radio"
        className="quiz-option__input"
        name={name}
        value={value}
        defaultChecked={defaultChecked}
        checked={checked}
        onChange={onChange}
        disabled={disabled}
      />
      {marker != null && <span className="quiz-option__marker" aria-hidden="true">{marker}</span>}
      <span className="quiz-option__label">{label}</span>
    </label>
  )
}
