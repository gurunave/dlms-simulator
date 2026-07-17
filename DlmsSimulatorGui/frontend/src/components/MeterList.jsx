export default function MeterList({ meters, selectedId, onSelect, onStart, onStop, onDelete }) {
  return (
    <div className="card meter-list">
      <h3>Meters <span className="count">{meters.length}</span></h3>
      {meters.length === 0 && <p className="muted">No meters yet.</p>}
      <ul>
        {meters.map((m) => (
          <li
            key={m.id}
            className={m.id === selectedId ? 'selected' : ''}
            onClick={() => onSelect(m.id)}
          >
            <div className="meter-main">
              <span className={`status-dot ${m.status.toLowerCase()}`} title={m.status} />
              <div className="meter-text">
                <div className="meter-name">{m.name}</div>
                <div className="meter-meta">
                  :{m.port} · SN {m.serial} · {m.interface}
                  {m.authenticationLevel && m.authenticationLevel !== 'None'
                    ? <span className="auth-badge" title={`Authentication: ${m.authenticationLevel}`}> · 🔒 {m.authenticationLevel}</span>
                    : <span className="auth-badge open" title="No authentication"> · 🔓 Open</span>}
                  {m.clientCount > 0 && <span className="clients"> · {m.clientCount} client{m.clientCount > 1 ? 's' : ''}</span>}
                </div>
                {m.error && <div className="meter-error">{m.error}</div>}
              </div>
            </div>
            <div className="meter-actions" onClick={(e) => e.stopPropagation()}>
              {m.status === 'Running' ? (
                <button className="ghost" onClick={() => onStop(m.id)} title="Stop">■</button>
              ) : (
                <button className="ghost play" onClick={() => onStart(m.id)} title="Start">▶</button>
              )}
              <button className="ghost danger" onClick={() => onDelete(m.id)} title="Delete">✕</button>
            </div>
          </li>
        ))}
      </ul>
    </div>
  );
}
