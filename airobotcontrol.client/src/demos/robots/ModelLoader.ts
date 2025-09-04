import { GLTFLoader } from 'three/examples/jsm/loaders/GLTFLoader.js';
import type { ModelBase, ModelSpec } from './types';

export class ModelLoader {
  private loader: GLTFLoader;
  private modelCache: Map<string, ModelBase>;

  constructor() {
    this.loader = new GLTFLoader();
    this.loader.setCrossOrigin('anonymous');
    this.modelCache = new Map();
  }

  public async loadModel(spec: ModelSpec): Promise<ModelBase> {
    if (this.modelCache.has(spec.key)) {
      return this.modelCache.get(spec.key)!;
    }

    try {
      const gltf = await new Promise<any>((resolve, reject) => {
        this.loader.load(
          spec.url,
          resolve,
          undefined,
          reject
        );
      });

      if (spec.scale) {
        gltf.scene.scale.setScalar(spec.scale);
      }

      const base: ModelBase = {
        scene: gltf.scene,
        animations: gltf.animations,
        key: spec.key
      };

      this.modelCache.set(spec.key, base);
      return base;
    } catch (error) {
      console.error(`Failed to load model ${spec.key}:`, error);
      throw error;
    }
  }

  public async loadModels(specs: ModelSpec[]): Promise<ModelBase[]> {
    return Promise.all(specs.map(spec => this.loadModel(spec)));
  }

  public getLoadedModel(key: string): ModelBase | undefined {
    return this.modelCache.get(key);
  }

  public clearCache(): void {
    this.modelCache.clear();
  }

  public dispose(): void {
    this.modelCache.clear();
  }
}