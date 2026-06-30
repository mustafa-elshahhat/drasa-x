import { Avatar } from '../ui/Avatar'

// Leaderboard row matching the prototype `lbRow`. The top three ranks get a
// medal-tinted marker; the current user's row is highlighted. All values
// (rank, points) come from the backend leaderboard — none are invented.
export function LeaderboardRow({ rank, name, points, pointsLabel, isMe = false }) {
  const medal = rank >= 1 && rank <= 3 ? `is-medal-${rank}` : ''
  return (
    <li className={`domain-lb${isMe ? ' is-me' : ''}`}>
      <span className={`domain-lb__rank ${medal}`.trim()} aria-hidden="true">{rank}</span>
      <Avatar name={name} size={34} />
      <span className="domain-lb__name">{name}</span>
      {points != null && (
        <span className="domain-lb__points">
          {points}
          {pointsLabel && <span className="domain-lb__points-label"> {pointsLabel}</span>}
        </span>
      )}
    </li>
  )
}
