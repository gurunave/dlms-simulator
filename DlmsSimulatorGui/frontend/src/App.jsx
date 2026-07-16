import { useEffect, useRef, useState, useCallback } from 'react';
import { api } from './api.js';
import { connectHub } from './signalr.js';
import MeterList from './components/MeterList.jsx';
import CreateMeter from './components/CreateMeter.jsx';
import ObjectTable from './components/ObjectTable.jsx';
import ActivityLog from './components/ActivityLog.jsx';

export default function App() {
  const [meters, setMeters] = useState([]);
  const [templates, setTemplates] = useState([]);
  const [selectedId, setSelectedId] = useState(null);
  const [hubState, setHubState] = useState('connecting');
  const [activity, setActivity] = useState([]);
  const [error, setError] = useState(null);
  const hubRef = useRef(null);

  const refreshMeters = useCallback(async () => {
    try {
      setMeters(await api.meters());
    } catch (e) {
      setError(e.message);
    }
  }, []);

  const refreshTemplates = useCallback(async () => {
    try {
      setTemplates(await api.templates());
    } catch (e) {
      setError(e.message);
    }
  }, []);

  useEffect(() => {
    refreshMeters();
    refreshTemplates();
    hubRef.current = connectHub({
      onState: setHubState,
      onStatus: (info) => {
        setMeters((prev) => prev.map((m) => (m.id === info.id ? info : m)));
      },
      onActivity: (ev) => {
        setActivity((prev) => [ev, ...prev].slice(0, 300));
      },
    });
    return () => hubRef.current && hubRef.current.stop();
  }, [refreshMeters, refreshTemplates]);

  const selected = meters.find((m) => m.id === selectedId) || null;

  async function withError(fn) {
    setError(null);
    try {
      await fn();
    } catch (e) {
      setError(e.message);
    }
  }

  const onCreate = (body) =>
    withError(async () => {
      const m = await api.createMeter(body);
      await refreshMeters();
      setSelectedId(m.id);
    });

  const onStart = (id) => withError(async () => { await api.startMeter(id); await refreshMeters(); });
  const onStop = (id) => withError(async () => { await api.stopMeter(id); await refreshMeters(); });
  const onDelete = (id) =>
    withError(async () => {
      await api.deleteMeter(id);
      if (selectedId === id) setSelectedId(null);
      await refreshMeters();
    });

  return (
    <div className="app">
      <header className="topbar">
        <div className="brand">
          <span className="logo">⚡</span>
          <div>
            <h1>DLMS Simulator</h1>
            <span className="subtitle">Gurux DLMS/COSEM meter simulator</span>
          </div>
        </div>
        <div className={`hub-status ${hubState}`}>
          <span className="dot" /> {hubState}
        </div>
      </header>

      {error && (
        <div className="banner error" onClick={() => setError(null)}>
          {error} <span className="dismiss">✕</span>
        </div>
      )}

      <div className="layout">
        <aside className="sidebar">
          <CreateMeter templates={templates} onCreate={onCreate} onUploaded={refreshTemplates} />
          <MeterList
            meters={meters}
            selectedId={selectedId}
            onSelect={setSelectedId}
            onStart={onStart}
            onStop={onStop}
            onDelete={onDelete}
          />
        </aside>

        <main className="content">
          {selected ? (
            <ObjectTable meter={selected} />
          ) : (
            <div className="empty">
              <div className="empty-icon">🔌</div>
              <h2>No meter selected</h2>
              <p>Create a meter or pick one from the list to view its COSEM objects.</p>
            </div>
          )}
        </main>

        <aside className="activity-pane">
          <ActivityLog activity={activity} meters={meters} onClear={() => setActivity([])} />
        </aside>
      </div>
    </div>
  );
}
