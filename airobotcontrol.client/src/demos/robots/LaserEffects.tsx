import React, { useState, useEffect } from 'react';
import { signalRService } from '../../services/SignalRService';

interface LaserEffect {
  id: string;
  fromX: number;
  fromY: number;
  toX: number;
  toY: number;
  startTime: number;
}

interface LaserEffectsProps {
  containerRef: React.RefObject<HTMLDivElement | null>;
}

const LaserEffects: React.FC<LaserEffectsProps> = ({ containerRef }) => {
  const [lasers, setLasers] = useState<LaserEffect[]>([]);

  useEffect(() => {
    const handleRobotAttack = (_attackerId: number, _targetId: number, _damage: number) => {
      if (!containerRef.current) return;

      // In a real implementation, you'd get the actual positions from your 3D scene
      // For now, we'll create a mock laser effect
      const containerRect = containerRef.current.getBoundingClientRect();
      const fromX = Math.random() * containerRect.width;
      const fromY = Math.random() * containerRect.height;
      const toX = Math.random() * containerRect.width;
      const toY = Math.random() * containerRect.height;

      const laserId = `laser-${Date.now()}-${Math.random()}`;
      const newLaser: LaserEffect = {
        id: laserId,
        fromX,
        fromY,
        toX,
        toY,
        startTime: Date.now()
      };

      setLasers(prev => [...prev, newLaser]);

      // Remove laser after animation completes
      setTimeout(() => {
        setLasers(prev => prev.filter(laser => laser.id !== laserId));
      }, 500);
    };

    signalRService.on('robotAttack', handleRobotAttack);

    return () => {
      signalRService.off('robotAttack', handleRobotAttack);
    };
  }, [containerRef]);

  const calculateLaserStyle = (laser: LaserEffect) => {
    const length = Math.sqrt(
      Math.pow(laser.toX - laser.fromX, 2) + Math.pow(laser.toY - laser.fromY, 2)
    );
    const angle = Math.atan2(laser.toY - laser.fromY, laser.toX - laser.fromX);

    return {
      position: 'absolute' as const,
      left: laser.fromX,
      top: laser.fromY,
      width: length,
      height: 2,
      background: 'linear-gradient(90deg, transparent, #ff4444, #ff8888, transparent)',
      transformOrigin: '0 50%',
      transform: `rotate(${angle}rad)`,
      zIndex: 997,
      pointerEvents: 'none' as const,
      opacity: 1,
      animation: 'laserShot 0.5s ease-in-out forwards'
    };
  };

  if (!containerRef.current) return null;

  return (
    <div
      style={{
        position: 'absolute',
        top: 0,
        left: 0,
        width: '100%',
        height: '100%',
        pointerEvents: 'none',
        zIndex: 997
      }}
    >
      {lasers.map(laser => (
        <div
          key={laser.id}
          style={calculateLaserStyle(laser)}
        />
      ))}
      <style>{`
        @keyframes laserShot {
          0% {
            opacity: 0;
            transform: rotate(${0}rad) scaleX(0);
          }
          20% {
            opacity: 1;
            transform: rotate(${0}rad) scaleX(0.2);
          }
          100% {
            opacity: 0;
            transform: rotate(${0}rad) scaleX(1);
          }
        }
      `}</style>
    </div>
  );
};

export default LaserEffects;