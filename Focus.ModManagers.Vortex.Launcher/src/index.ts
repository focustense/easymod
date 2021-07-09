import { IExtensionContext } from 'vortex-api/lib/types/IExtensionContext';
import { writeFileSync } from 'fs';
import { tmpdir } from 'os';
import { basename, join } from 'path';
import { actions } from 'vortex-api';

enum GameId {
  SSE = 'skyrimse',
}

interface IModAttributes {
  modId: number;
  modName: string;
}

interface IFileInfo {
  modId: number;
}

interface IModInfo {
  name: string;
}

interface IBootstrapFile {
  files: Record<string, IFileInfo>;
  mods: Record<number, IModInfo>;
}

const init = (context: IExtensionContext) => {
  function createLookupFile(): string {
    const mods = context.api.getState().persistent.mods[GameId.SSE] || {};
    const data: IBootstrapFile = { files: {}, mods: {} };
    for (const mod of Object.values(mods)) {
      const attributes = mod.attributes as IModAttributes;
      if (!attributes || !attributes.modId)
        continue;
      data.files[mod.id] = { modId: attributes.modId };
      data.mods[attributes.modId] = { name: attributes.modName };
    }
    const lookupPath = join(tmpdir(), 'easynpc-vortex-bootstrap.json');
    writeFileSync(lookupPath, JSON.stringify(data));
    return lookupPath;
  }

  function launchEasyNpc() {
    const dataPath = createLookupFile();

    const tools = context.api.getState().settings.gameMode.discovered[GameId.SSE]?.tools || {};
    const easyNpcTool = Object.values(tools).find(t => basename(t.path).toLowerCase() == "easynpc.exe");
    if (easyNpcTool) {
      context.api.runExecutable(
        easyNpcTool.path,
        [`--vortex-manifest="${dataPath}"`],
        {
          shell: false,
          suggestDeploy: false,
          onSpawned: context.api.store.dispatch(actions.setToolRunning(easyNpcTool.path, Date.now(), true)),
        })
        .catch(err => {
          if (err.errno === 'ENOENT') {
            context.api.showErrorNotification(
              'EasyNPC executable not found',
              `The path '${easyNpcTool.path}' is invalid. Check the tool configuration in the Vortex dashboard.`);
          } else {
            console.log(err);
          }
        })
    } else {
      context.api.showErrorNotification(
        'EasyNPC not configured',
        'Could not find a registered tool named EasyNPC.exe. Check that this tool is configured in the Vortex dashboard.' +
        JSON.stringify(tools));
    }
  }

  context.registerAction('mod-icons', 999, 'launch-simple', {}, 'Launch EasyNPC', () => launchEasyNpc());
};

export default init;