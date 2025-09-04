import * as THREE from 'three';
import * as SkeletonUtils from 'three/examples/jsm/utils/SkeletonUtils.js';
import type { CharacterConfig } from './types';
import { DEFAULT_GAME_CONFIG } from './types';
import { AnimationManager } from './AnimationManager';
import { HealthBar } from './HealthBar';

export class Character {
  private name: string;
  private root: THREE.Object3D;
  private animationManager: AnimationManager;
  private healthBar: HealthBar;
  
  private moveDirection: THREE.Vector3;
  private faceMesh: THREE.Mesh | null = null;
  private morphDict: Record<string, number> | null = null;
  
  private health: number;
  private maxHealth: number;
  private speedWalk: number;
  private speedRun: number;
  private isSprinting: boolean = false;
  private grounded: boolean = true;
  private verticalVelocity: number = 0;
  private gravity: number;
  private attackCooldown: number = 0;
  
  public isPlayer: boolean = false;
  public aiWanderTarget: THREE.Vector3 | null = null;
  public enemyTarget: Character | null = null;
  public dead: boolean = false;

  constructor(config: CharacterConfig, scene: THREE.Scene) {
    this.name = config.name;
    
    this.root = SkeletonUtils.clone(config.base.scene);
    this.root.traverse((object) => {
      if ((object as THREE.Mesh).isMesh) {
        const mesh = object as THREE.Mesh;
        mesh.castShadow = true;
        mesh.frustumCulled = false;
      }
    });
    
    if (config.position) {
      this.root.position.copy(config.position);
    }
    
    scene.add(this.root);
    
    this.animationManager = new AnimationManager(this.root, config.base.animations);
    
    this.moveDirection = new THREE.Vector3();
    
    this.speedWalk = DEFAULT_GAME_CONFIG.walkSpeed;
    this.speedRun = DEFAULT_GAME_CONFIG.runSpeed;
    this.gravity = DEFAULT_GAME_CONFIG.gravity;
    
    this.maxHealth = DEFAULT_GAME_CONFIG.maxHealth;
    this.health = this.maxHealth;
    
    this.healthBar = new HealthBar(this.maxHealth);
    this.healthBar.setPosition(new THREE.Vector3(0, 2.4, 0));
    this.root.add(this.healthBar.getGroup());
    
    this.adjustHealthBarHeight();
    this.setupMorphTargets();
    
    this.isPlayer = config.isPlayer || false;
    
    this.animationManager.play('idle', 0.001);
  }

  private adjustHealthBarHeight(): void {
    const bbox = new THREE.Box3().setFromObject(this.root);
    const height = bbox.max.y - bbox.min.y;
    if (isFinite(height) && height > 0) {
      this.healthBar.setPosition(new THREE.Vector3(0, height + 0.35, 0));
    }
  }

  private setupMorphTargets(): void {
    this.root.traverse((object) => {
      if ((object as THREE.Mesh).isMesh) {
        const mesh = object as THREE.Mesh;
        if (mesh.morphTargetDictionary && !this.faceMesh) {
          this.faceMesh = mesh;
          this.morphDict = mesh.morphTargetDictionary;
        }
      }
    });
  }

  public setEmotion(name: string, weight: number = 1, duration: number = 0.25): void {
    if (!this.faceMesh || !this.morphDict) return;
    
    const keys = Object.keys(this.morphDict);
    const index = keys.findIndex(k => k.toLowerCase().includes(name.toLowerCase()));
    
    if (index >= 0 && this.faceMesh.morphTargetInfluences) {
      const target = this.faceMesh.morphTargetInfluences[index] ?? 0;
      const start = target;
      let elapsed = 0;
      
      const animate = (delta: number) => {
        elapsed += delta;
        const alpha = Math.min(elapsed / duration, 1);
        if (this.faceMesh?.morphTargetInfluences) {
          this.faceMesh.morphTargetInfluences[index] = start + (weight - start) * alpha;
        }
        return alpha < 1;
      };
      
      const update = () => {
        if (animate(0.016)) {
          requestAnimationFrame(update);
        }
      };
      update();
    }
  }

  public clearEmotions(duration: number = 0.3): void {
    if (!this.faceMesh || !this.morphDict || !this.faceMesh.morphTargetInfluences) return;
    
    const influences = this.faceMesh.morphTargetInfluences;
    const start = influences.map(v => v || 0);
    let elapsed = 0;
    
    const animate = (delta: number) => {
      elapsed += delta;
      const alpha = Math.min(elapsed / duration, 1);
      for (let i = 0; i < influences.length; i++) {
        influences[i] = start[i] * (1 - alpha);
      }
      return alpha < 1;
    };
    
    const update = () => {
      if (animate(0.016)) {
        requestAnimationFrame(update);
      }
    };
    update();
  }

  public setHealth(health: number): void {
    this.health = Math.max(0, Math.min(this.maxHealth, health));
    this.healthBar.updateHealth(this.health);
    
    if (this.health <= 0 && !this.dead) {
      this.animationManager.play('death', 0.1);
      this.dead = true;
      this.enemyTarget = null;
    }
  }

  public damage(amount: number, attacker?: Character): void {
    if (this.dead) return;
    
    this.setHealth(this.health - amount);
    this.setEmotion('angry', 1, 0.18);
    
    setTimeout(() => this.clearEmotions(), 500);
    
    if (!this.isPlayer && attacker && !attacker.dead) {
      this.enemyTarget = attacker;
    }
  }

  public tryAttack(target: Character): void {
    if (!target || target.dead) return;
    if (this.attackCooldown > 0) return;
    
    const distance = this.root.position.distanceTo(target.getPosition());
    if (distance < DEFAULT_GAME_CONFIG.attackRange) {
      this.animationManager.play('attack', 0.1);
      
      const damage = DEFAULT_GAME_CONFIG.attackDamageMin + 
        Math.random() * (DEFAULT_GAME_CONFIG.attackDamageMax - DEFAULT_GAME_CONFIG.attackDamageMin);
      
      target.damage(damage, this);
      this.attackCooldown = DEFAULT_GAME_CONFIG.attackCooldown;
    }
  }

  public jump(): void {
    if (!this.grounded) return;
    
    this.grounded = false;
    this.verticalVelocity = DEFAULT_GAME_CONFIG.jumpVelocity;
    this.animationManager.play('jump', 0.05, 1.2);
  }

  public update(delta: number, cameraQuaternion?: THREE.Quaternion): void {
    if (this.dead) {
      this.animationManager.update(delta);
      if (cameraQuaternion) {
        this.healthBar.updateOrientation(cameraQuaternion);
      }
      return;
    }
    
    if (this.attackCooldown > 0) {
      this.attackCooldown -= delta;
    }
    
    if (!this.grounded) {
      this.verticalVelocity -= this.gravity * delta;
      this.root.position.y += this.verticalVelocity * delta;
      
      if (this.root.position.y <= 0) {
        this.root.position.y = 0;
        this.grounded = true;
        this.verticalVelocity = 0;
      }
    }
    
    const speed = this.isSprinting ? this.speedRun : this.speedWalk;
    const moveLength = this.moveDirection.length();
    
    if (moveLength > 0.001) {
      const direction = this.moveDirection.clone().normalize();
      const step = direction.multiplyScalar(speed * delta);
      this.root.position.add(step);
      
      const position = this.root.position;
      if (position.length() > DEFAULT_GAME_CONFIG.boundsRadius) {
        position.setLength(DEFAULT_GAME_CONFIG.boundsRadius - 0.1);
      }
      
      const facing = Math.atan2(this.moveDirection.x, this.moveDirection.z);
      const targetQuat = new THREE.Quaternion().setFromAxisAngle(
        new THREE.Vector3(0, 1, 0), 
        facing
      );
      this.root.quaternion.slerp(targetQuat, 1 - Math.pow(0.0001, delta));
      
      const fast = this.isSprinting || moveLength > 0.6;
      if (fast) {
        this.animationManager.play('run', 0.08);
      } else {
        this.animationManager.play('walk', 0.08);
      }
    } else {
      this.animationManager.play('idle', 0.2);
    }
    
    if (this.enemyTarget && !this.enemyTarget.dead) {
      const toTarget = this.enemyTarget.getPosition().clone().sub(this.root.position);
      const distance = toTarget.length();
      
      if (distance > 2.0) {
        this.moveDirection.copy(toTarget.normalize());
      } else {
        this.moveDirection.set(0, 0, 0);
        this.tryAttack(this.enemyTarget);
      }
    }
    
    if (cameraQuaternion) {
      this.healthBar.updateOrientation(cameraQuaternion);
    }
    
    this.animationManager.update(delta);
    
    if (this.isPlayer) {
      this.moveDirection.set(0, 0, 0);
    }
  }

  public setMoveDirection(direction: THREE.Vector3): void {
    this.moveDirection.copy(direction);
  }

  public setSprinting(sprinting: boolean): void {
    this.isSprinting = sprinting;
  }

  public getPosition(): THREE.Vector3 {
    return this.root.position.clone();
  }

  public getName(): string {
    return this.name;
  }

  public getRoot(): THREE.Object3D {
    return this.root;
  }

  public isGrounded(): boolean {
    return this.grounded;
  }

  public dispose(): void {
    this.animationManager.dispose();
    this.healthBar.dispose();
    this.root.parent?.remove(this.root);
  }
}