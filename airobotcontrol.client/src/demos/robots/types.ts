import * as THREE from 'three';

export interface Vector3Data {
  x: number;
  y: number;
  z: number;
}

export interface CharacterConfig {
  base: ModelBase;
  name: string;
  position?: THREE.Vector3;
  isPlayer?: boolean;
}

export interface ModelBase {
  scene: THREE.Object3D;
  animations: THREE.AnimationClip[];
  key: string;
}

export interface ModelSpec {
  key: string;
  url: string;
  scale?: number;
}

export interface AnimationClips {
  idle?: THREE.AnimationClip;
  walk?: THREE.AnimationClip;
  run?: THREE.AnimationClip;
  jump?: THREE.AnimationClip;
  attack?: THREE.AnimationClip;
  death?: THREE.AnimationClip;
}

export interface CharacterActions {
  idle?: THREE.AnimationAction;
  walk?: THREE.AnimationAction;
  run?: THREE.AnimationAction;
  jump?: THREE.AnimationAction;
  attack?: THREE.AnimationAction;
  death?: THREE.AnimationAction;
}

export type CharacterState = 'idle' | 'walking' | 'running' | 'jumping' | 'attacking' | 'dead';

export interface GameConfig {
  boundsRadius: number;
  gravity: number;
  walkSpeed: number;
  runSpeed: number;
  jumpVelocity: number;
  attackRange: number;
  attackCooldown: number;
  attackDamageMin: number;
  attackDamageMax: number;
  maxHealth: number;
}

export const DEFAULT_GAME_CONFIG: GameConfig = {
  boundsRadius: 70,
  gravity: 18,
  walkSpeed: 3.2,
  runSpeed: 6.2,
  jumpVelocity: 8.5,
  attackRange: 2.2,
  attackCooldown: 0.7,
  attackDamageMin: 8,
  attackDamageMax: 18,
  maxHealth: 100,
};

export const ACTION_KEYS = {
  idle: ['idle', 'standing', 'stand'],
  walk: ['walk', 'walking'],
  run: ['run', 'running', 'sprint'],
  jump: ['jump'],
  attack: ['punch', 'attack', 'hit', 'swing'],
  death: ['death', 'die'],
  yes: ['yes', 'thumbsup'],
  no: ['no', 'headshake'],
  wave: ['wave'],
};