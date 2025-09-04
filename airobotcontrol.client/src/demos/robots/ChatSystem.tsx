import React, { useState, useEffect, useRef } from 'react';
import { signalRService } from '../../services/SignalRService';
import type { ChatMessage } from '../../services/SignalRService';
import './ChatSystem.css';

interface ChatSystemProps {
  visible: boolean;
  onToggle: () => void;
}

interface DisplayMessage extends ChatMessage {
  id: string;
  robotName?: string;
  isVisible: boolean;
}

const ChatSystem: React.FC<ChatSystemProps> = ({ visible, onToggle }) => {
  const [messages, setMessages] = useState<DisplayMessage[]>([]);
  const [robotNames, setRobotNames] = useState<{ [key: number]: string }>({});
  const messagesEndRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const handleMessageReceived = (message: ChatMessage) => {
      const displayMessage: DisplayMessage = {
        ...message,
        id: `${message.fromRobotId}-${Date.now()}-${Math.random()}`,
        robotName: robotNames[message.fromRobotId] || `Robot ${message.fromRobotId}`,
        isVisible: true
      };

      setMessages(prev => {
        const newMessages = [...prev, displayMessage];
        // Keep only last 50 messages
        return newMessages.slice(-50);
      });

      // Auto-hide message after 10 seconds
      setTimeout(() => {
        setMessages(prev => 
          prev.map(msg => 
            msg.id === displayMessage.id 
              ? { ...msg, isVisible: false }
              : msg
          )
        );
      }, 10000);
    };

    const handleRobotConnected = (robotId: number, robotName: string) => {
      setRobotNames(prev => ({ ...prev, [robotId]: robotName }));
    };

    const handleMessageRateLimited = () => {
      // You could show a notification here
      console.log('Message rate limited');
    };

    signalRService.on('messageReceived', handleMessageReceived);
    signalRService.on('robotConnected', handleRobotConnected);
    signalRService.on('messageRateLimited', handleMessageRateLimited);

    return () => {
      signalRService.off('messageReceived', handleMessageReceived);
      signalRService.off('robotConnected', handleRobotConnected);
      signalRService.off('messageRateLimited', handleMessageRateLimited);
    };
  }, [robotNames]);

  useEffect(() => {
    // Scroll to bottom when new messages arrive
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  const visibleMessages = messages.filter(msg => msg.isVisible);

  return (
    <>
      {/* Chat toggle button */}
      <button 
        className={`chat-toggle ${visible ? 'active' : ''}`}
        onClick={onToggle}
        title="Toggle Chat"
      >
        💬
      </button>

      {/* Chat panel */}
      {visible && (
        <div className="chat-panel">
          <div className="chat-header">
            <h3>Robot Chat</h3>
            <button className="chat-close" onClick={onToggle}>×</button>
          </div>
          <div className="chat-messages">
            {visibleMessages.length === 0 ? (
              <div className="no-messages">No messages yet...</div>
            ) : (
              visibleMessages.map(message => (
                <div key={message.id} className="chat-message">
                  <div className="message-header">
                    <span className="robot-name">{message.robotName}</span>
                    <span className="message-time">
                      {new Date(message.timestamp).toLocaleTimeString()}
                    </span>
                  </div>
                  <div className="message-content">{message.message}</div>
                  <div className="message-visibility">
                    Visible to: {message.visibleToRobots.map(id => 
                      robotNames[id] || `Robot ${id}`
                    ).join(', ')}
                  </div>
                </div>
              ))
            )}
            <div ref={messagesEndRef} />
          </div>
        </div>
      )}

      {/* Floating chat bubbles */}
      <div className="floating-chat-bubbles">
        {messages
          .filter(msg => msg.isVisible)
          .slice(-3) // Show only last 3 messages as floating bubbles
          .map(message => (
            <div 
              key={`bubble-${message.id}`} 
              className="chat-bubble"
              style={{
                animation: 'fadeInOut 10s ease-in-out forwards'
              }}
            >
              <div className="bubble-header">
                {message.robotName}
              </div>
              <div className="bubble-content">
                {message.message}
              </div>
            </div>
          ))}
      </div>
    </>
  );
};

export default ChatSystem;