import * as signalR from '@microsoft/signalr';

// Creates and starts a SignalR connection to the simulator hub.
// onActivity: fired for connect/disconnect/read/write events.
// onStatus:   fired when a meter's status/client-count changes.
export function connectHub({ onActivity, onStatus, onState }) {
  const conn = new signalR.HubConnectionBuilder()
    .withUrl('/hub/simulator')
    .withAutomaticReconnect()
    .configureLogging(signalR.LogLevel.Warning)
    .build();

  conn.on('activity', (ev) => onActivity && onActivity(ev));
  conn.on('meterStatus', (info) => onStatus && onStatus(info));

  conn.onreconnecting(() => onState && onState('reconnecting'));
  conn.onreconnected(() => onState && onState('connected'));
  conn.onclose(() => onState && onState('disconnected'));

  conn
    .start()
    .then(() => onState && onState('connected'))
    .catch(() => onState && onState('disconnected'));

  return conn;
}
