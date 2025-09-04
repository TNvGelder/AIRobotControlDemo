import * as THREE from 'three';
import type { AnimationClips, CharacterActions } from './types';
import { ACTION_KEYS } from './types';

export class AnimationManager {
  private mixer: THREE.AnimationMixer;
  private animations: THREE.AnimationClip[];
  private clips: AnimationClips;
  private actions: CharacterActions;
  private currentAction: THREE.AnimationAction | null = null;

  constructor(root: THREE.Object3D, animations: THREE.AnimationClip[]) {
    this.mixer = new THREE.AnimationMixer(root);
    this.animations = animations;
    this.clips = {};
    this.actions = {};
    
    this.setupAnimations();
  }

  private findClip(hints: string[]): THREE.AnimationClip | null {
    const names = this.animations.map(a => a.name.toLowerCase());
    for (const hint of hints) {
      const index = names.findIndex(n => n.includes(hint.toLowerCase()));
      if (index !== -1) return this.animations[index];
    }
    return null;
  }

  private setupAnimations(): void {
    this.clips = {
      idle: this.findClip(ACTION_KEYS.idle) || this.animations[0],
      walk: this.findClip(ACTION_KEYS.walk) || this.findClip(ACTION_KEYS.idle) || this.animations[0],
      run: this.findClip(ACTION_KEYS.run) || this.findClip(ACTION_KEYS.walk) || this.animations[0],
      jump: this.findClip(ACTION_KEYS.jump) || undefined,
      attack: this.findClip(ACTION_KEYS.attack) || 
              this.findClip(ACTION_KEYS.wave) || 
              this.findClip(ACTION_KEYS.yes) || undefined,
      death: this.findClip(ACTION_KEYS.death) || undefined,
    };

    for (const [key, clip] of Object.entries(this.clips)) {
      if (clip) {
        const action = this.mixer.clipAction(clip);
        if (key === 'jump' || key === 'attack' || key === 'death') {
          action.clampWhenFinished = true;
          action.loop = THREE.LoopOnce;
        }
        this.actions[key as keyof CharacterActions] = action;
      }
    }
  }

  public play(
    name: keyof CharacterActions, 
    fadeTime: number = 0.2, 
    timeScale: number = 1
  ): void {
    const action = this.actions[name];
    if (!action) return;
    if (this.currentAction === action) return;

    if (this.currentAction) {
      this.currentAction.fadeOut(fadeTime);
    }

    action.reset();
    action.setEffectiveTimeScale(timeScale)
      .setEffectiveWeight(1)
      .fadeIn(fadeTime)
      .play();
    
    this.currentAction = action;
  }

  public update(delta: number): void {
    this.mixer.update(delta);
  }

  public stopAll(): void {
    this.mixer.stopAllAction();
    this.currentAction = null;
  }

  public dispose(): void {
    this.mixer.stopAllAction();
    this.mixer.uncacheRoot(this.mixer.getRoot());
  }

  public getMixer(): THREE.AnimationMixer {
    return this.mixer;
  }

  public getCurrentAction(): THREE.AnimationAction | null {
    return this.currentAction;
  }
}