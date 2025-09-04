import * as THREE from 'three';
import { Character } from './Character';
import { DEFAULT_GAME_CONFIG } from './types';

export class AIController {
  private character: Character;
  private boundsRadius: number;

  constructor(character: Character, boundsRadius: number = DEFAULT_GAME_CONFIG.boundsRadius) {
    this.character = character;
    this.boundsRadius = boundsRadius;
  }

  public think(): void {
    if (this.character.enemyTarget && !this.character.enemyTarget.dead) {
      return;
    }

    if (!this.character.aiWanderTarget || 
        this.character.getPosition().distanceTo(this.character.aiWanderTarget) < 1.0) {
      
      if (Math.random() < 0.2) {
        this.character.setMoveDirection(new THREE.Vector3(0, 0, 0));
        return;
      }

      const angle = Math.random() * Math.PI * 2;
      const radius = this.randomRange(10, this.boundsRadius - 8);
      const x = Math.cos(angle) * radius;
      const z = Math.sin(angle) * radius;
      this.character.aiWanderTarget = new THREE.Vector3(x, 0, z);
    }

    const toTarget = this.character.aiWanderTarget.clone()
      .sub(this.character.getPosition());
    
    this.character.setMoveDirection(toTarget.setY(0).normalize());
    this.character.setSprinting(false);
  }

  private randomRange(min: number, max: number): number {
    return min + Math.random() * (max - min);
  }

  public setTarget(target: Character | null): void {
    this.character.enemyTarget = target;
  }

  public clearWanderTarget(): void {
    this.character.aiWanderTarget = null;
  }

  public dispose(): void {
    this.character.aiWanderTarget = null;
    this.character.enemyTarget = null;
  }
}