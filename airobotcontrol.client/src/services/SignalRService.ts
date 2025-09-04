import * as signalR from '@microsoft/signalr';

export interface ChatMessage {
  fromRobotId: number;
  message: string;
  timestamp: string;
  visibleToRobots: number[];
}

export interface RobotStateUpdate {
  health: number;
  energy: number;
  maxEnergy: number;
  happiness: number;
  x: number;
  y: number;
  z: number;
}

export interface Battery {
  id: number;
  x: number;
  y: number;
  z: number;
  energy: number;
}

export class SignalRService {
  private connection: signalR.HubConnection | null = null;
  private eventListeners: { [key: string]: ((...args: any[]) => void)[] } = {};
  
  async connect(): Promise<void> {
    if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
      return;
    }

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl('/robotHub')
      .withAutomaticReconnect()
      .build();

    // Set up event handlers
    this.connection.on('RobotConnected', (robotId: number, robotName: string) => {
      this.emit('robotConnected', robotId, robotName);
    });

    this.connection.on('RobotDisconnected', (robotId: number) => {
      this.emit('robotDisconnected', robotId);
    });

    this.connection.on('ReceiveMessage', (message: ChatMessage) => {
      this.emit('messageReceived', message);
    });

    this.connection.on('MessageRateLimited', () => {
      this.emit('messageRateLimited');
    });

    this.connection.on('RobotPositionUpdated', (robotId: number, x: number, y: number, z: number) => {
      this.emit('robotPositionUpdated', robotId, x, y, z);
    });

    this.connection.on('RobotEnteredRange', (robotId: number) => {
      this.emit('robotEnteredRange', robotId);
    });

    this.connection.on('RobotLeftRange', (robotId: number) => {
      this.emit('robotLeftRange', robotId);
    });

    this.connection.on('RobotAttack', (attackerId: number, targetId: number, damage: number) => {
      this.emit('robotAttack', attackerId, targetId, damage);
    });

    this.connection.on('RobotStateUpdated', (robotId: number, state: RobotStateUpdate) => {
      this.emit('robotStateUpdated', robotId, state);
    });

    this.connection.on('BatteryCollected', (robotId: number, batteryId: number) => {
      this.emit('batteryCollected', robotId, batteryId);
    });

    this.connection.on('BatterySpawned', (batteryId: number, x: number, y: number, z: number, energy: number) => {
      this.emit('batterySpawned', batteryId, x, y, z, energy);
    });

    this.connection.on('RobotGroupChanged', (robotId: number, newGroupId: number | null, oldGroupId: number | null) => {
      this.emit('robotGroupChanged', robotId, newGroupId, oldGroupId);
    });

    await this.connection.start();
    console.log('SignalR connected');
  }

  async disconnect(): Promise<void> {
    if (this.connection) {
      await this.connection.stop();
      this.connection = null;
    }
  }

  async registerRobot(robotId: number, robotName: string): Promise<void> {
    if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
      await this.connection.invoke('RegisterRobot', robotId, robotName);
    }
  }

  async sendMessage(fromRobotId: number, message: string, visibleToRobots: number[]): Promise<void> {
    if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
      await this.connection.invoke('SendMessage', fromRobotId, message, visibleToRobots);
    }
  }

  async updateRobotPosition(robotId: number, x: number, y: number, z: number): Promise<void> {
    if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
      await this.connection.invoke('UpdateRobotPosition', robotId, x, y, z);
    }
  }

  async robotEnteredChatRange(robotId: number, otherRobotId: number): Promise<void> {
    if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
      await this.connection.invoke('RobotEnteredChatRange', robotId, otherRobotId);
    }
  }

  async robotLeftChatRange(robotId: number, otherRobotId: number): Promise<void> {
    if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
      await this.connection.invoke('RobotLeftChatRange', robotId, otherRobotId);
    }
  }

  async attackRobot(attackerId: number, targetId: number, damage: number): Promise<void> {
    if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
      await this.connection.invoke('AttackRobot', attackerId, targetId, damage);
    }
  }

  async updateRobotState(robotId: number, state: RobotStateUpdate): Promise<void> {
    if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
      await this.connection.invoke('UpdateRobotState', robotId, state);
    }
  }

  async batteryCollected(robotId: number, batteryId: number): Promise<void> {
    if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
      await this.connection.invoke('BatteryCollected', robotId, batteryId);
    }
  }

  async robotSwitchedGroup(robotId: number, newGroupId: number | null, oldGroupId: number | null): Promise<void> {
    if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
      await this.connection.invoke('RobotSwitchedGroup', robotId, newGroupId, oldGroupId);
    }
  }

  // Event system
  on(event: string, listener: (...args: any[]) => void): void {
    if (!this.eventListeners[event]) {
      this.eventListeners[event] = [];
    }
    this.eventListeners[event].push(listener);
  }

  off(event: string, listener: (...args: any[]) => void): void {
    if (this.eventListeners[event]) {
      this.eventListeners[event] = this.eventListeners[event].filter(l => l !== listener);
    }
  }

  private emit(event: string, ...args: any[]): void {
    if (this.eventListeners[event]) {
      this.eventListeners[event].forEach(listener => listener(...args));
    }
  }

  get isConnected(): boolean {
    return this.connection?.state === signalR.HubConnectionState.Connected;
  }
}

// Singleton instance
export const signalRService = new SignalRService();