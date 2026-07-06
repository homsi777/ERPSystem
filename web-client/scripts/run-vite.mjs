import { spawn, spawnSync } from 'node:child_process';
import { existsSync } from 'node:fs';
import { resolve } from 'node:path';

const command = process.argv[2] ?? 'dev';
const passthroughArgs = process.argv.slice(3);
const projectRoot = process.cwd();
const viteBin = resolve(projectRoot, 'node_modules', 'vite', 'bin', 'vite.js');

if (!existsSync(viteBin)) {
  console.error('Vite is not installed. Run npm install first.');
  process.exit(1);
}

const needsCleanPath = projectRoot.includes('#');
const cleanCwd = needsCleanPath ? ensureSubstDrive(projectRoot) : projectRoot;
const cleanViteBin = needsCleanPath ? resolve(cleanCwd, 'node_modules', 'vite', 'bin', 'vite.js') : viteBin;
const child = spawn(process.execPath, [cleanViteBin, command, ...passthroughArgs], {
  cwd: cleanCwd,
  stdio: 'inherit',
  shell: false
});

function cleanup() {
  if (needsCleanPath) {
    spawnSync('cmd.exe', ['/c', 'subst', cleanCwd.slice(0, 2), '/D'], { stdio: 'ignore' });
  }
}

child.on('exit', (code, signal) => {
  cleanup();
  if (signal) {
    process.kill(process.pid, signal);
    return;
  }
  process.exit(code ?? 0);
});

process.on('SIGINT', () => {
  child.kill('SIGINT');
});

process.on('SIGTERM', () => {
  child.kill('SIGTERM');
});

function ensureSubstDrive(target) {
  const drives = ['W:', 'V:', 'U:', 'T:', 'S:', 'R:'];
  for (const drive of drives) {
    const existing = spawnSync('cmd.exe', ['/c', 'if', 'exist', `${drive}\\`, 'echo', 'exists'], {
      encoding: 'utf8'
    }).stdout.trim();

    if (existing) {
      continue;
    }

    const result = spawnSync('cmd.exe', ['/c', 'subst', drive, target], {
      stdio: 'ignore'
    });

    if (result.status === 0) {
      return `${drive}\\`;
    }
  }

  console.error('Could not create a temporary clean drive for Vite. Project path contains #.');
  process.exit(1);
}
