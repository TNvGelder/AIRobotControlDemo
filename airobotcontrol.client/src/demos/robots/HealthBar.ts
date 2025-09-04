import * as THREE from 'three';

export class HealthBar {
  private group: THREE.Group;
  private foreground: THREE.Mesh;
  private maxHealth: number;
  private currentHealth: number;
  private width: number;

  constructor(maxHealth: number = 100, width: number = 1.8, height: number = 0.22) {
    this.maxHealth = maxHealth;
    this.currentHealth = maxHealth;
    this.width = width;
    
    this.group = new THREE.Group();
    
    const geometry = new THREE.PlaneGeometry(width, height);
    
    const background = new THREE.Mesh(
      geometry,
      new THREE.MeshBasicMaterial({ 
        color: 0x461e2e, 
        transparent: true, 
        opacity: 0.85 
      })
    );
    
    const foregroundGeometry = new THREE.PlaneGeometry(width, height);
    this.foreground = new THREE.Mesh(
      foregroundGeometry,
      new THREE.MeshBasicMaterial({ 
        color: 0x3ef38a, 
        transparent: true, 
        opacity: 0.95 
      })
    );
    
    this.foreground.position.z = 0.001;
    this.foreground.position.x = -width / 2;
    this.foreground.scale.x = 1.0;
    
    const frame = new THREE.Mesh(
      geometry,
      new THREE.MeshBasicMaterial({ 
        color: 0x000000, 
        transparent: true, 
        opacity: 0.2, 
        side: THREE.DoubleSide, 
        wireframe: true 
      })
    );
    frame.position.z = 0.002;
    
    this.group.add(background, this.foreground, frame);
    this.group.visible = true;
  }

  public updateHealth(health: number): void {
    this.currentHealth = Math.max(0, Math.min(this.maxHealth, health));
    const ratio = this.currentHealth / this.maxHealth;
    this.foreground.scale.x = Math.max(0, Math.min(1, ratio));
    this.foreground.position.x = -this.width / 2 + (this.width / 2) * ratio;
    
    const healthPercentage = ratio * 100;
    if (healthPercentage > 60) {
      (this.foreground.material as THREE.MeshBasicMaterial).color.setHex(0x3ef38a);
    } else if (healthPercentage > 30) {
      (this.foreground.material as THREE.MeshBasicMaterial).color.setHex(0xf3e83e);
    } else {
      (this.foreground.material as THREE.MeshBasicMaterial).color.setHex(0xf33e3e);
    }
  }

  public setPosition(position: THREE.Vector3): void {
    this.group.position.copy(position);
  }

  public updateOrientation(quaternion: THREE.Quaternion): void {
    this.group.quaternion.copy(quaternion);
  }

  public getGroup(): THREE.Group {
    return this.group;
  }

  public getHealth(): number {
    return this.currentHealth;
  }

  public getMaxHealth(): number {
    return this.maxHealth;
  }

  public dispose(): void {
    this.group.traverse((child) => {
      if (child instanceof THREE.Mesh) {
        child.geometry.dispose();
        (child.material as THREE.Material).dispose();
      }
    });
  }
}