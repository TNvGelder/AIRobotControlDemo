import React, { useState, useEffect } from 'react';
import { signalRService } from '../../services/SignalRService';
import type { Battery } from '../../services/SignalRService';

interface BatteryDisplayProps {
  visible: boolean;
}

const BatteryDisplay: React.FC<BatteryDisplayProps> = ({ visible }) => {
  const [batteries, setBatteries] = useState<{ [key: number]: Battery }>({});
  const [collectedBatteries, setCollectedBatteries] = useState<{ [key: number]: number }>({});

  useEffect(() => {
    const handleBatterySpawned = (batteryId: number, x: number, y: number, z: number, energy: number) => {
      setBatteries(prev => ({
        ...prev,
        [batteryId]: { id: batteryId, x, y, z, energy }
      }));
    };

    const handleBatteryCollected = (robotId: number, batteryId: number) => {
      setCollectedBatteries(prev => ({
        ...prev,
        [batteryId]: robotId
      }));
      
      // Remove from batteries after a short delay
      setTimeout(() => {
        setBatteries(prev => {
          const newBatteries = { ...prev };
          if (newBatteries[batteryId]) {
            newBatteries[batteryId] = { ...newBatteries[batteryId], energy: 0 };
          }
          return newBatteries;
        });
        
        setCollectedBatteries(prev => {
          const newCollected = { ...prev };
          delete newCollected[batteryId];
          return newCollected;
        });
      }, 2000);
    };

    signalRService.on('batterySpawned', handleBatterySpawned);
    signalRService.on('batteryCollected', handleBatteryCollected);

    return () => {
      signalRService.off('batterySpawned', handleBatterySpawned);
      signalRService.off('batteryCollected', handleBatteryCollected);
    };
  }, []);

  if (!visible) return null;

  const activeBatteries = Object.values(batteries).filter(b => b.energy > 0);
  const emptyBatteries = Object.values(batteries).filter(b => b.energy === 0);

  return (
    <div className="battery-indicator">
      <h4 style={{ margin: '0 0 8px 0', fontSize: '14px' }}>🔋 Batteries</h4>
      <div className="battery-list">
        {activeBatteries.length > 0 && (
          <>
            <div style={{ fontSize: '12px', opacity: 0.7, marginBottom: '4px' }}>Active:</div>
            {activeBatteries.map(battery => (
              <div key={battery.id} className="battery-item">
                <span>#{battery.id}</span>
                <span className="battery-energy">{battery.energy.toFixed(0)}%</span>
                <span style={{ fontSize: '10px', opacity: 0.6 }}>
                  ({battery.x.toFixed(0)}, {battery.z.toFixed(0)})
                </span>
                {collectedBatteries[battery.id] && (
                  <span className="battery-collected">
                    Collected by Robot {collectedBatteries[battery.id]}
                  </span>
                )}
              </div>
            ))}
          </>
        )}
        
        {emptyBatteries.length > 0 && (
          <>
            <div style={{ fontSize: '12px', opacity: 0.7, marginBottom: '4px', marginTop: '8px' }}>
              Respawning:
            </div>
            {emptyBatteries.slice(0, 3).map(battery => (
              <div key={battery.id} className="battery-item battery-empty">
                <span>#{battery.id}</span>
                <span>Empty</span>
              </div>
            ))}
          </>
        )}
        
        {activeBatteries.length === 0 && emptyBatteries.length === 0 && (
          <div style={{ fontSize: '12px', opacity: 0.5, fontStyle: 'italic' }}>
            No batteries detected
          </div>
        )}
      </div>
    </div>
  );
};

export default BatteryDisplay;