import { useCallback, useEffect, useState } from 'react';
import { api } from '../api.js';

export default function ObjectTable({ meter }) {
  const [objects, setObjects] = useState([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [filter, setFilter] = useState('');
  const [edit, setEdit] = useState(null); // { ln, index, value }

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      setObjects(await api.objects(meter.id));
    } catch (e) {
      setError(e.message);
      setObjects([]);
    } finally {
      setLoading(false);
    }
  }, [meter.id]);

  useEffect(() => { load(); }, [load]);

  async function saveEdit() {
    if (!edit) return;
    try {
      const updated = await api.setAttribute(meter.id, edit.ln, edit.index, edit.value);
      setObjects((prev) => prev.map((o) => (o.logicalName === updated.logicalName ? updated : o)));
      setEdit(null);
    } catch (e) {
      setError(e.message);
    }
  }

  const running = meter.status === 'Running';
  const term = filter.trim().toLowerCase();
  const shown = term
    ? objects.filter(
        (o) =>
          o.logicalName.toLowerCase().includes(term) ||
          o.objectType.toLowerCase().includes(term)
      )
    : objects;

  return (
    <div className="objects">
      <div className="objects-head">
        <div>
          <h2>{meter.name}</h2>
          <div className="objects-sub">
            <span className={`status-dot ${meter.status.toLowerCase()}`} />
            {meter.status} · port {meter.port} · {meter.template} · {objects.length} objects
          </div>
        </div>
        <div className="objects-tools">
          <input placeholder="Filter by OBIS / type…" value={filter} onChange={(e) => setFilter(e.target.value)} />
          <button className="ghost" onClick={load} title="Refresh">⟳</button>
        </div>
      </div>

      {error && <div className="banner error">{error}</div>}
      {!running && (
        <div className="banner info">Meter is stopped — values are read from the template file and can’t be edited until you start it.</div>
      )}
      {loading ? (
        <div className="muted pad">Loading objects…</div>
      ) : (
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Type</th>
                <th>OBIS (Logical Name)</th>
                <th>Attr</th>
                <th>Value</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {shown.map((o) =>
                o.attributes.map((a, ai) => {
                  const editing = edit && edit.ln === o.logicalName && edit.index === a.index;
                  const editable = running && a.index > 1; // attr 1 is the logical name
                  return (
                    <tr key={o.logicalName + ':' + a.index}>
                      {ai === 0 ? (
                        <>
                          <td rowSpan={o.attributes.length} className="type">{o.objectType}</td>
                          <td rowSpan={o.attributes.length} className="obis">{o.logicalName}</td>
                        </>
                      ) : null}
                      <td className="attr">{a.index} <span className="attr-name">{a.name}</span></td>
                      <td className="value">
                        {editing ? (
                          <input
                            autoFocus
                            value={edit.value ?? ''}
                            onChange={(e) => setEdit({ ...edit, value: e.target.value })}
                            onKeyDown={(e) => {
                              if (e.key === 'Enter') saveEdit();
                              if (e.key === 'Escape') setEdit(null);
                            }}
                          />
                        ) : (
                          <span className="value-text">{a.value ?? <em className="null">null</em>}</span>
                        )}
                      </td>
                      <td className="edit-cell">
                        {editing ? (
                          <>
                            <button className="ghost play" onClick={saveEdit} title="Save">✔</button>
                            <button className="ghost" onClick={() => setEdit(null)} title="Cancel">✕</button>
                          </>
                        ) : (
                          editable && (
                            <button
                              className="ghost tiny"
                              onClick={() => setEdit({ ln: o.logicalName, index: a.index, value: a.value })}
                              title="Edit value"
                            >
                              ✎
                            </button>
                          )
                        )}
                      </td>
                    </tr>
                  );
                })
              )}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
