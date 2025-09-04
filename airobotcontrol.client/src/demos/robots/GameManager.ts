import * as THREE from 'three';
import { SceneManager } from './SceneManager';
import { Character } from './Character';
import { AIController } from './AIController';
import { InputManager } from './InputManager';
import { ModelLoader } from './ModelLoader';
import type { ModelSpec } from './types';

export class GameManager {
  private sceneManager: SceneManager;
  private inputManager: InputManager;
  private modelLoader: ModelLoader;
  private characters: Character[] = [];
  private aiControllers: Map<Character, AIController> = new Map();
  private player: Character | null = null;
  private cameraFollow: boolean = true;
  private isRunning: boolean = false;
  private animationFrameId: number | null = null;

  constructor(container: HTMLElement) {
    this.sceneManager = new SceneManager(container);
    this.modelLoader = new ModelLoader();
    this.inputManager = new InputManager(
      this.sceneManager.getCamera(),
      this.sceneManager.getRenderer(),
      this.characters
    );

    this.inputManager.onAttack((attacker, target) => {
      console.log(`${attacker.getName()} is engaging ${target.getName()}!`);
    });
  }

  public async initialize(): Promise<void> {
    const modelSpecs: ModelSpec[] = [
      { 
        key: 'RobotExpressive', 
        url: 'https://threejs.org/examples/models/gltf/RobotExpressive/RobotExpressive.glb', 
        scale: 1.0 
      },
      { 
        key: 'Xbot', 
        url: 'https://threejs.org/examples/models/gltf/Xbot.glb', 
        scale: 1.0 
      },
      { 
        key: 'Soldier', 
        url: 'https://threejs.org/examples/models/gltf/Soldier.glb', 
        scale: 1.0 
      },
    ];

    const bases = await this.modelLoader.loadModels(modelSpecs);
    
    const spawnPoints = [
      new THREE.Vector3(-10, 0, -10),
      new THREE.Vector3(12, 0, -8),
      new THREE.Vector3(-8, 0, 12),
      new THREE.Vector3(8, 0, 8),
      new THREE.Vector3(-14, 0, 6),
    ];

    for (let i = 0; i < bases.length; i++) {
      const character = new Character(
        {
          base: bases[i],
          name: `${bases[i].key}_${i + 1}`,
          position: spawnPoints[i] || this.getRandomSpawnPoint(),
          isPlayer: i === 0
        },
        this.sceneManager.getScene()
      );

      this.characters.push(character);

      if (!character.isPlayer) {
        const aiController = new AIController(character);
        this.aiControllers.set(character, aiController);
      }
    }

    const extraXbot = new Character(
      {
        base: bases[1],
        name: 'Xbot_Extra',
        position: spawnPoints[3],
        isPlayer: false
      },
      this.sceneManager.getScene()
    );
    this.characters.push(extraXbot);
    this.aiControllers.set(extraXbot, new AIController(extraXbot));

    const extraRobot = new Character(
      {
        base: bases[0],
        name: 'RobotExpressive_Extra',
        position: spawnPoints[4],
        isPlayer: false
      },
      this.sceneManager.getScene()
    );
    this.characters.push(extraRobot);
    this.aiControllers.set(extraRobot, new AIController(extraRobot));

    if (this.characters.length > 0) {
      this.setControlledCharacter(this.characters[0]);
    }
  }

  private getRandomSpawnPoint(): THREE.Vector3 {
    const angle = Math.random() * Math.PI * 2;
    const radius = Math.random() * 20;
    return new THREE.Vector3(
      Math.cos(angle) * radius,
      0,
      Math.sin(angle) * radius
    );
  }

  public setControlledCharacter(character: Character): void {
    if (this.player) {
      this.player.isPlayer = false;
    }

    this.player = character;
    character.isPlayer = true;
    character.enemyTarget = null;

    this.inputManager.setPlayer(character);

    const controls = this.sceneManager.getControls();
    controls.target.copy(character.getPosition());
    controls.target.y += 1.5;
    controls.update();
  }

  public getCharacters(): Character[] {
    return this.characters;
  }

  public getPlayer(): Character | null {
    return this.player;
  }

  public setCameraFollow(enabled: boolean): void {
    this.cameraFollow = enabled;
  }

  public getCameraFollow(): boolean {
    return this.cameraFollow;
  }

  public start(): void {
    if (this.isRunning) return;
    this.isRunning = true;
    this.animate();
  }

  public stop(): void {
    this.isRunning = false;
    if (this.animationFrameId !== null) {
      cancelAnimationFrame(this.animationFrameId);
      this.animationFrameId = null;
    }
  }

  private animate = (): void => {
    if (!this.isRunning) return;

    const delta = this.sceneManager.getClock().getDelta();
    
    this.inputManager.updatePlayerInput();

    for (const character of this.characters) {
      if (!character.isPlayer && !character.dead) {
        const aiController = this.aiControllers.get(character);
        if (aiController) {
          aiController.think();
        }
      }
      
      character.update(delta, this.sceneManager.getCamera().quaternion);
    }

    if (this.player && this.cameraFollow) {
      const targetPosition = this.player.getPosition().clone();
      targetPosition.y += 1.5;
      this.sceneManager.updateCameraTarget(targetPosition, delta, true);
    }

    this.sceneManager.render();
    
    this.animationFrameId = requestAnimationFrame(this.animate);
  };

  public dispose(): void {
    this.stop();
    
    for (const character of this.characters) {
      character.dispose();
    }
    
    for (const aiController of this.aiControllers.values()) {
      aiController.dispose();
    }
    
    this.characters = [];
    this.aiControllers.clear();
    
    this.inputManager.dispose();
    this.modelLoader.dispose();
    this.sceneManager.dispose();
  }
}