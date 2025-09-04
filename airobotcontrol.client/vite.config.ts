import { fileURLToPath, URL } from 'node:url';

import { defineConfig } from 'vite';
import plugin from '@vitejs/plugin-react';
import fs from 'fs';
import path from 'path';
import child_process from 'child_process';
import { env } from 'process';

// Detect environment
const isCodespaces = env.CODESPACES === 'true';
const isDevContainer = env.REMOTE_CONTAINERS === 'true' || !!env.DEVCONTAINER;
const isContainer = isCodespaces || isDevContainer;

// Configure host based on environment
const host = isContainer ? '0.0.0.0' : (env.VITE_HOST || 'localhost');
const useHttps = env.VITE_USE_HTTPS !== 'false' && !isContainer;

// Certificate configuration for HTTPS (only for non-container environments)
let httpsConfig = undefined;
if (useHttps) {
    const baseFolder =
        env.APPDATA !== undefined && env.APPDATA !== ''
            ? `${env.APPDATA}/ASP.NET/https`
            : `${env.HOME}/.aspnet/https`;

    const certificateName = "airobotcontrol.client";
    const certFilePath = path.join(baseFolder, `${certificateName}.pem`);
    const keyFilePath = path.join(baseFolder, `${certificateName}.key`);

    if (!fs.existsSync(baseFolder)) {
        fs.mkdirSync(baseFolder, { recursive: true });
    }

    if (!fs.existsSync(certFilePath) || !fs.existsSync(keyFilePath)) {
        const result = child_process.spawnSync('dotnet', [
            'dev-certs',
            'https',
            '--export-path',
            certFilePath,
            '--format',
            'Pem',
            '--no-password',
        ], { stdio: 'inherit' });
        
        if (result.status !== 0) {
            console.warn("Could not create certificate. Falling back to HTTP.");
        } else {
            httpsConfig = {
                key: fs.readFileSync(keyFilePath),
                cert: fs.readFileSync(certFilePath),
            };
        }
    } else {
        httpsConfig = {
            key: fs.readFileSync(keyFilePath),
            cert: fs.readFileSync(certFilePath),
        };
    }
}

// Dynamic target configuration
const getProxyTarget = () => {
    // First check for explicit environment variable
    if (env.VITE_API_URL) {
        return env.VITE_API_URL;
    }
    
    // Check ASP.NET Core environment variables
    if (env.ASPNETCORE_HTTPS_PORT) {
        return useHttps 
            ? `https://localhost:${env.ASPNETCORE_HTTPS_PORT}`
            : `http://localhost:${env.ASPNETCORE_HTTPS_PORT}`;
    }
    
    if (env.ASPNETCORE_URLS) {
        return env.ASPNETCORE_URLS.split(';')[0];
    }
    
    // Default based on environment
    return isContainer 
        ? 'http://localhost:7038'
        : 'http://localhost:5209';
};

const target = getProxyTarget();

console.log(`Vite configuration:
  Environment: ${isContainer ? (isCodespaces ? 'Codespaces' : 'DevContainer') : 'Local'}
  Host: ${host}
  HTTPS: ${useHttps}
  Proxy Target: ${target}
`);

// https://vitejs.dev/config/
export default defineConfig({
    plugins: [plugin()],
    resolve: {
        alias: {
            '@': fileURLToPath(new URL('./src', import.meta.url))
        }
    },
    server: {
        host,
        proxy: {
            '^/weatherforecast': {
                target,
                secure: false,
                changeOrigin: isContainer
            },
            '^/api': {
                target,
                secure: false,
                changeOrigin: isContainer
            },
            '^/robotHub': {
                target,
                secure: false,
                changeOrigin: isContainer,
                ws: true
            }
        },
        port: parseInt(env.DEV_SERVER_PORT || env.VITE_PORT || '51164'),
        https: httpsConfig
    }
})