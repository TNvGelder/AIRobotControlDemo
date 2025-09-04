import React, { useEffect, useRef, useState } from 'react';
import { GameManager } from './GameManager';
import { Character } from './Character';
import { signalRService } from '../../services/SignalRService';
import ChatSystem from './ChatSystem';
import LaserEffects from './LaserEffects';
import BatteryDisplay from './BatteryDisplay';
import './RobotsDemo.css';

const RobotsDemo: React.FC = () => {
  const containerRef = useRef<HTMLDivElement>(null);
  const gameManagerRef = useRef<GameManager | null>(null);
  const [, setIsLoading] = useState(true);
  const [characters, setCharacters] = useState<Character[]>([]);
  const [currentPlayer, setCurrentPlayer] = useState<Character | null>(null);
  const [cameraFollow, setCameraFollow] = useState(true);
  const [statusMessage, setStatusMessage] = useState('Loading models from threejs.org...');
  const [toastMessage, setToastMessage] = useState<string | null>(null);
  const [chatVisible, setChatVisible] = useState(false);
  const [batteryDisplayVisible, setBatteryDisplayVisible] = useState(true);
  const [isSignalRConnected, setIsSignalRConnected] = useState(false);
  const toastTimeoutRef = useRef<NodeJS.Timeout | null>(null);

  useEffect(() => {
    if (!containerRef.current) return;

    // Initialize SignalR connection
    const initSignalR = async () => {
      try {
        setStatusMessage('Connecting to server...');
        await signalRService.connect();
        setIsSignalRConnected(true);
        console.log('SignalR connected successfully');
      } catch (error) {
        console.error('Failed to connect SignalR:', error);
        setStatusMessage('Failed to connect to server. Some features may not work.');
      }
    };

    // Wait a frame to ensure the container has dimensions
    const timeoutId = setTimeout(async () => {
      const initGame = async () => {
        try {
          await initSignalR();
          
          setStatusMessage('Initializing game...');
          const gameManager = new GameManager(containerRef.current!);
          gameManagerRef.current = gameManager;

          setStatusMessage('Loading models from threejs.org...');
          await gameManager.initialize();

          setStatusMessage('Spawning characters...');
          const chars = gameManager.getCharacters();
          setCharacters(chars);
          setCurrentPlayer(gameManager.getPlayer());

          // Register robots with SignalR
          chars.forEach(async (char, index) => {
            if (signalRService.isConnected) {
              await signalRService.registerRobot(index + 1, char.getName());
            }
          });

          gameManager.start();
          setIsLoading(false);
          setStatusMessage('Ready — possess, move, fight, and chat!');
          showToast('Left-click a target to attack. Chat system enabled!');
        } catch (error) {
          console.error('Failed to initialize game:', error);
          setStatusMessage('Failed to load game. Please refresh the page.');
        }
      };

      initGame();
    }, 100);

    return () => {
      clearTimeout(timeoutId);
      if (gameManagerRef.current) {
        gameManagerRef.current.dispose();
        gameManagerRef.current = null;
      }
      signalRService.disconnect();
    };
  }, []);

  const showToast = (message: string) => {
    setToastMessage(message);
    if (toastTimeoutRef.current) {
      clearTimeout(toastTimeoutRef.current);
    }
    toastTimeoutRef.current = setTimeout(() => {
      setToastMessage(null);
    }, 2000);
  };

  const handleTakeControl = (character: Character) => {
    if (gameManagerRef.current) {
      gameManagerRef.current.setControlledCharacter(character);
      setCurrentPlayer(character);
      showToast(`You are now controlling ${character.getName()}.`);
    }
  };

  const handleCameraFollowToggle = () => {
    if (gameManagerRef.current) {
      const newState = !cameraFollow;
      gameManagerRef.current.setCameraFollow(newState);
      setCameraFollow(newState);
      showToast(`Camera follow ${newState ? 'enabled' : 'disabled'}.`);
    }
  };

  const handleChatToggle = () => {
    setChatVisible(!chatVisible);
  };

  const handleBatteryDisplayToggle = () => {
    setBatteryDisplayVisible(!batteryDisplayVisible);
  };

  return (
    <div className="robots-demo">
      <div ref={containerRef} className="game-container" />
      
      {/* Real-time features */}
      <LaserEffects containerRef={containerRef} />
      <ChatSystem visible={chatVisible} onToggle={handleChatToggle} />
      <BatteryDisplay visible={batteryDisplayVisible} />
      
      <div className="overlay">
        <div className="ui-panel">
          <h1>🤖 Robots Playground</h1>
          <p className="subtitle">
            Right-drag to rotate camera • Mouse wheel to zoom • Click to attack • 
            WASD/Arrows to move • <span className="kbd">Shift</span> sprint • 
            <span className="kbd">Space</span> jump
          </p>
          
          <div className="controls-row">
            <span className="tag">
              Controlling: {currentPlayer ? currentPlayer.getName() : '—'}
            </span>
            <span className={`tag ${isSignalRConnected ? 'connected' : 'disconnected'}`}>
              Server: {isSignalRConnected ? 'Connected' : 'Disconnected'}
            </span>
          </div>

          <div className="controls-row">
            <button 
              className={`button ${cameraFollow ? 'primary' : ''}`}
              onClick={handleCameraFollowToggle}
            >
              Camera Follow: {cameraFollow ? 'On' : 'Off'}
            </button>
            <button 
              className={`button ${batteryDisplayVisible ? 'primary' : ''}`}
              onClick={handleBatteryDisplayToggle}
            >
              Batteries: {batteryDisplayVisible ? 'On' : 'Off'}
            </button>
          </div>

          <div className="character-list">
            {characters.map((character) => (
              <div key={character.getName()} className="character-card">
                <div>
                  <strong>{character.getName()}</strong>
                </div>
                <button 
                  className="button"
                  onClick={() => handleTakeControl(character)}
                  disabled={character === currentPlayer}
                >
                  {character === currentPlayer ? 'Controlled' : 'Take Control'}
                </button>
              </div>
            ))}
          </div>
        </div>

        <div className="help-panel">
          <div className="help-card">
            <strong>Goal</strong>: Possess any robot and try bonking another — 
            they will fight back. All characters (player/NPC) share the same 
            movement and animation system with smooth crossfades.
            <div className="status">{statusMessage}</div>
          </div>
        </div>

        {toastMessage && (
          <div className="toast">
            <div className="toast-message">{toastMessage}</div>
          </div>
        )}
      </div>
    </div>
  );
};

export default RobotsDemo;