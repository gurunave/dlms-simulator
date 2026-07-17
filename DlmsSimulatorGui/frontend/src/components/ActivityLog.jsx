const KIND_META = {
  connected: { icon: '🔗', cls: 'connected', label: 'Client connected' },
  disconnected: { icon: '⛔', cls: 'disconnected', label: 'Client disconnected' },
  read: { icon: '📤', cls: 'read', label: 'Read' },
  write: { icon: '✏️', cls: 'write', label: 'Write' },
  status: { icon: 'ℹ️', cls: 'status', label: 'Status' },
  auth: { icon: '🔒', cls: 'auth', label: 'Authentication failed' },
};

export default function ActivityLog({ activity, meters, onClear }) {
  const nameOf = (id) => meters.find((m) => m.id === id)?.name || id;

  return (
    <div className="card activity">
      <div className="activity-head">
        <h3>Live activity</h3>
        <button className="ghost tiny" onClick={onClear} title="Clear">Clear</button>
      </div>
      {activity.length === 0 && <p className="muted">Waiting for client traffic…</p>}
      <ul>
        {activity.map((ev, i) => {
          const meta = KIND_META[ev.kind] || KIND_META.status;
          return (
            <li key={i} className={`ev ${meta.cls}`}>
              <span className="ev-icon">{meta.icon}</span>
              <div className="ev-body">
                <div className="ev-top">
                  <span className="ev-meter">{nameOf(ev.meterId)}</span>
                  <span className="ev-time">{ev.time}</span>
                </div>
                <div className="ev-detail">
                  {ev.kind === 'read' || ev.kind === 'write' ? (
                    <>
                      <code>{ev.logicalName}:{ev.index}</code>
                      {ev.value != null && <span className="ev-val"> = {ev.value}</span>}
                    </>
                  ) : (
                    <span>{meta.label}{ev.detail ? ` · ${ev.detail}` : ''}</span>
                  )}
                </div>
              </div>
            </li>
          );
        })}
      </ul>
    </div>
  );
}
