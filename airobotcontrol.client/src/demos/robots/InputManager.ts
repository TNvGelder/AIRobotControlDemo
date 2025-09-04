import * as THREE from 'three';
import { Character } from './Character';

export class InputManager {
  private keyState: Map<string, boolean>;
  private player: Character | null = null;
  private camera: THREE.PerspectiveCamera;
  private raycaster: THREE.Raycaster;
  private mouse: THREE.Vector2;
  private characters: Character[];
  private renderer: THREE.WebGLRenderer;
  private onAttackCallback?: (attacker: Character, target: Character) => void;

  constructor(
    camera: THREE.PerspectiveCamera, 
    renderer: THREE.WebGLRenderer, 
    characters: Character[]
  ) {
    this.keyState = new Map();
    this.camera = camera;
    this.renderer = renderer;
    this.characters = characters;
    this.raycaster = new THREE.Raycaster();
    this.mouse = new THREE.Vector2();

    this.setupEventListeners();
  }

  private setupEventListeners(): void {
    window.addEventListener('keydown', (e) => {
      this.keyState.set(e.code, true);
    });

    window.addEventListener('keyup', (e) => {
      this.keyState.set(e.code, false);
    });

    this.renderer.domElement.addEventListener('pointerdown', (event) => {
      this.handlePointerDown(event);
    });
  }

  private handlePointerDown(event: PointerEvent): void {
    if (event.button !== 0) return;
    if (!this.player || this.player.dead) return;

    const rect = this.renderer.domElement.getBoundingClientRect();
    this.mouse.x = ((event.clientX - rect.left) / rect.width) * 2 - 1;
    this.mouse.y = -((event.clientY - rect.top) / rect.height) * 2 + 1;

    this.raycaster.setFromCamera(this.mouse, this.camera);

    const meshes: THREE.Mesh[] = [];
    for (const character of this.characters) {
      if (character === this.player) continue;
      
      character.getRoot().traverse((object) => {
        if ((object as THREE.Mesh).isMesh) {
          meshes.push(object as THREE.Mesh);
        }
      });
    }

    const intersects = this.raycaster.intersectObjects(meshes, true);
    
    if (intersects.length > 0) {
      let target: Character | null = null;
      const hitObject = intersects[0].object;

      for (const character of this.characters) {
        if (character === this.player) continue;
        
        let parent: THREE.Object3D | null = hitObject;
        while (parent) {
          if (parent === character.getRoot()) {
            target = character;
            break;
          }
          parent = parent.parent;
        }
        
        if (target) break;
      }

      if (target) {
        this.player.enemyTarget = target;
        if (this.onAttackCallback) {
          this.onAttackCallback(this.player, target);
        }
      }
    }
  }

  public updatePlayerInput(): void {
    if (!this.player || this.player.dead) return;

    let forward = 0;
    let strafe = 0;

    if (this.keyState.get('KeyW') || this.keyState.get('ArrowUp')) {
      forward += 1;
    }
    if (this.keyState.get('KeyS') || this.keyState.get('ArrowDown')) {
      forward -= 1;
    }
    if (this.keyState.get('KeyA') || this.keyState.get('ArrowLeft')) {
      strafe -= 1;
    }
    if (this.keyState.get('KeyD') || this.keyState.get('ArrowRight')) {
      strafe += 1;
    }

    const cameraDirection = new THREE.Vector3();
    this.camera.getWorldDirection(cameraDirection);
    cameraDirection.y = 0;
    cameraDirection.normalize();

    const cameraRight = new THREE.Vector3()
      .crossVectors(new THREE.Vector3(0, 1, 0), cameraDirection)
      .normalize()
      .negate();

    const moveVector = cameraDirection
      .multiplyScalar(forward)
      .add(cameraRight.multiplyScalar(strafe));

    if (moveVector.lengthSq() > 0) {
      this.player.setMoveDirection(moveVector.normalize());
    }

    const isSprinting = !!(
      this.keyState.get('ShiftLeft') || 
      this.keyState.get('ShiftRight')
    );
    this.player.setSprinting(isSprinting);

    if ((this.keyState.get('Space') || this.keyState.get('Numpad0')) && 
        this.player.isGrounded()) {
      this.player.jump();
    }
  }

  public setPlayer(player: Character | null): void {
    this.player = player;
  }

  public getPlayer(): Character | null {
    return this.player;
  }

  public onAttack(callback: (attacker: Character, target: Character) => void): void {
    this.onAttackCallback = callback;
  }

  public dispose(): void {
    window.removeEventListener('keydown', this.handleKeyDown);
    window.removeEventListener('keyup', this.handleKeyUp);
    this.renderer.domElement.removeEventListener('pointerdown', this.handlePointerDown);
  }

  private handleKeyDown = (e: KeyboardEvent) => {
    this.keyState.set(e.code, true);
  };

  private handleKeyUp = (e: KeyboardEvent) => {
    this.keyState.set(e.code, false);
  };
}