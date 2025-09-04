import * as THREE from 'three';
import { OrbitControls } from 'three/examples/jsm/controls/OrbitControls.js';

export class SceneManager {
  private scene: THREE.Scene;
  private camera: THREE.PerspectiveCamera;
  private renderer: THREE.WebGLRenderer;
  private controls: OrbitControls;
  private clock: THREE.Clock;

  constructor(container: HTMLElement) {
    this.clock = new THREE.Clock();
    
    this.renderer = new THREE.WebGLRenderer({ antialias: true });
    this.renderer.setPixelRatio(Math.min(2, window.devicePixelRatio));
    
    // Ensure container has dimensions
    const width = container.clientWidth || window.innerWidth;
    const height = container.clientHeight || window.innerHeight;
    
    this.renderer.setSize(width, height);
    this.renderer.shadowMap.enabled = true;
    container.appendChild(this.renderer.domElement);

    this.scene = new THREE.Scene();
    this.scene.background = new THREE.Color(0x0b1020);
    this.scene.fog = new THREE.Fog(0x0b1020, 40, 140);

    this.camera = new THREE.PerspectiveCamera(
      55,
      width / height,
      0.1,
      500
    );
    this.camera.position.set(0, 8, 18);

    this.controls = new OrbitControls(this.camera, this.renderer.domElement);
    this.controls.enablePan = false;
    this.controls.maxPolarAngle = Math.PI * 0.49;
    this.controls.minDistance = 6;
    this.controls.maxDistance = 60;
    this.controls.mouseButtons = { 
      LEFT: null as any, 
      MIDDLE: null as any, 
      RIGHT: THREE.MOUSE.ROTATE 
    };

    this.renderer.domElement.addEventListener('contextmenu', (e) => e.preventDefault());

    this.setupLighting();
    this.createEnvironment();
    
    window.addEventListener('resize', () => this.handleResize(container));
  }

  private setupLighting(): void {
    // Add ambient light to ensure something is visible
    const ambientLight = new THREE.AmbientLight(0x404040, 1.5);
    this.scene.add(ambientLight);
    
    const hemiLight = new THREE.HemisphereLight(0x98c1ff, 0x383838, 2.5);
    hemiLight.position.set(0, 40, 0);
    this.scene.add(hemiLight);

    const dirLight = new THREE.DirectionalLight(0xffffff, 2.0);
    dirLight.position.set(20, 30, 10);
    dirLight.castShadow = true;
    dirLight.shadow.mapSize.set(2048, 2048);
    dirLight.shadow.camera.top = 50;
    dirLight.shadow.camera.bottom = -50;
    dirLight.shadow.camera.left = -50;
    dirLight.shadow.camera.right = 50;
    dirLight.shadow.camera.near = 1;
    dirLight.shadow.camera.far = 120;
    this.scene.add(dirLight);
  }

  private createEnvironment(): void {
    const groundMaterial = new THREE.MeshStandardMaterial({ 
      color: 0x132149, 
      roughness: 0.95, 
      metalness: 0.0 
    });
    const ground = new THREE.Mesh(
      new THREE.CircleGeometry(160, 96), 
      groundMaterial
    );
    ground.receiveShadow = true;
    ground.rotation.x = -Math.PI / 2;
    this.scene.add(ground);

    const ringMaterial = new THREE.MeshStandardMaterial({ 
      color: 0x1b2d63, 
      roughness: 0.9 
    });
    
    for (let radius = 20; radius <= 140; radius += 20) {
      const torus = new THREE.Mesh(
        new THREE.TorusGeometry(radius, 0.25, 8, 128), 
        ringMaterial
      );
      torus.rotation.x = Math.PI / 2;
      torus.receiveShadow = true;
      torus.castShadow = true;
      this.scene.add(torus);
    }
  }

  private handleResize(container: HTMLElement): void {
    this.camera.aspect = container.clientWidth / container.clientHeight;
    this.camera.updateProjectionMatrix();
    this.renderer.setSize(container.clientWidth, container.clientHeight);
  }

  public getScene(): THREE.Scene {
    return this.scene;
  }

  public getCamera(): THREE.PerspectiveCamera {
    return this.camera;
  }

  public getRenderer(): THREE.WebGLRenderer {
    return this.renderer;
  }

  public getControls(): OrbitControls {
    return this.controls;
  }

  public getClock(): THREE.Clock {
    return this.clock;
  }

  public render(): void {
    this.renderer.render(this.scene, this.camera);
  }

  public updateCameraTarget(target: THREE.Vector3, delta: number, enabled: boolean): void {
    if (enabled) {
      const lerpedTarget = new THREE.Vector3();
      lerpedTarget.copy(this.controls.target);
      lerpedTarget.lerp(target, 1 - Math.pow(0.0001, delta));
      this.controls.target.copy(lerpedTarget);
    }
    this.controls.update();
  }

  public dispose(): void {
    this.renderer.dispose();
    this.controls.dispose();
  }
}