import { readFileSync, writeFileSync } from 'fs';
import { tmpdir } from 'os';
import { basename, join } from 'path';
import { actions } from 'vortex-api';
import { IExtensionContext } from 'vortex-api/lib/types/IExtensionContext';
import { IMod, IProfile } from 'vortex-api/lib/types/IState';

interface IModAttributes {
  fileId: string;
  modId: string;
  modName: string;
}

interface IFileInfo {
  id: string;
  isEnabled: boolean;
  modId: string;
}

interface IModInfo {
  name: string;
}

interface IBootstrapFile {
  files: Record<string, IFileInfo>;
  mods: Record<number, IModInfo>;
  reportPath: string;
  stagingDir: string;
}

interface IReportFile {
  modName: string;
}

const DUMMY_MOD_ID = 7777777;

// This really should come from Electron's app.getPath(), same as Vortex itself, but not sure how to
// use that without including the entire electron package.
// This implementation comes from https://stackoverflow.com/a/26227660/38360
const appDataDir = process.env.APPDATA || (process.platform == 'darwin' ?
  process.env.HOME + '/Library/Preferences' : process.env.HOME + "/.local/share");
const userDataDir = join(appDataDir, 'Vortex');

const init = (context: IExtensionContext) => {
  function activateMod(gameId: string, modName: string): Promise<void> {
    const now = new Date();
    const hasExistingMod = !!context.api.getState().persistent.mods[gameId]?.[modName];
    return hasExistingMod ? updateMod() : createMod();

    function createMod(): Promise<void> {
      const mod: IMod = {
        id: modName,
        attributes: {
          category: 33 /* NPC */,
          enableallplugins: true,
          installTime: now,
          logicalFileName: modName,
          modId: DUMMY_MOD_ID,
          modName: `EasyNPC Output - ${modName}`,
          name: modName,
          shortDescription: getShortDescription(),
          version: '1.0.0',
        },
        installationPath: modName,
        state: 'installed',
        type: '',
      };
      return new Promise<void>((resolve, reject) => {
        context.api.events.emit('create-mod', gameId, mod, async (error) => {
          if (error != null) {
            return reject(error);
          }
          resolve();
        });
      });
    }

    function getShortDescription() {
      return `NPC overhaul merge generated by EasyNPC on ` +
        `${now.getFullYear()}-${zeroPad(now.getMonth() + 1, 2)}-${zeroPad(now.getDay(), 2)} ` +
        `${zeroPad(now.getHours(), 2)}:${zeroPad(now.getMinutes(), 2)}:${zeroPad(now.getSeconds(), 2)}`;
    }

    function updateMod(): Promise<void> {
      const setAttribute = (name: string, value: unknown) =>
        context.api.store.dispatch(actions.setModAttribute(gameId, modName, name, value));
      setAttribute('category', 33);
      setAttribute('installTime', now);
      setAttribute('logicalFileName', modName);
      setAttribute('modId', DUMMY_MOD_ID);
      setAttribute('modName', `EasyNPC Output - ${modName}`);
      setAttribute('name', modName);
      setAttribute('shortDescription', getShortDescription());
      setAttribute('version', '1.0.0');
      return Promise.resolve();
    }
  }

  function createLookupFile(profile: IProfile, reportPath: string): string {
    const state = context.api.getState();
    const mods = state.persistent.mods[profile.gameId] || {};
    let stagingDir = state.settings.mods.installPath[profile.gameId]
      .replace(/{userdata}/ig, userDataDir)
      .replace(/{game}/ig, profile.gameId);
    const data: IBootstrapFile = {
      files: {},
      mods: {},
      reportPath,
      stagingDir,
    };
    for (const mod of Object.values(mods)) {
      const attributes = (mod.attributes || {}) as IModAttributes;
      data.files[mod.id] = {
        id: attributes.fileId,
        isEnabled: profile?.modState[mod.id]?.enabled ?? false,
        modId: attributes.modId,
      };
      if (attributes.modId) {
        data.mods[attributes.modId] = { name: attributes.modName };
      }
    }
    const lookupPath = join(tmpdir(), 'easynpc-vortex-bootstrap.json');
    writeFileSync(lookupPath, JSON.stringify(data));
    return lookupPath;
  }

  function getCurrentProfile(): IProfile {
    const state = context.api.getState();
    return state.persistent.profiles[state.settings.profiles.activeProfileId]
  }

  function isGameSupported(): boolean {
    const profile = getCurrentProfile();
    return ['skyrim', 'skyrimse', 'skyrimvr', 'enderal', 'enderalse', 'fallout4', 'fallout4vr']
      .includes(profile.gameId);
  }

  function launchEasyNpc(parameters?: string[]) {
    const reportPath = join(tmpdir(), 'easynpc-vortex-report.json');
    // Since we can't communicate with the process directly, writing a "sentinel" file before starting can give us a
    // better clue of what actually happened. If the file is unchanged, then most likely the tool was closed before
    // any attempt at a build. If the file is missing or empty, it means that a build was started, but either the
    // build itself failed or creating the report failed.
    const sentinelContent = 'sentinel';
    writeFileSync(reportPath, sentinelContent);

    const profile = getCurrentProfile();
    const dataPath = createLookupFile(profile, reportPath);

    const tools = context.api.getState().settings.gameMode.discovered[profile.gameId]?.tools || {};
    const easyNpcTool = Object.values(tools).find(t => t.path && basename(t.path).toLowerCase() == "easynpc.exe");
    if (easyNpcTool) {
      context.api.runExecutable(
        easyNpcTool.path,
        (easyNpcTool.parameters || []).concat(parameters || []).concat(
          [
            `--report-path=${reportPath}`,
            `--vortex-manifest="${dataPath}"`,
          ]),
        {
          shell: false,
          suggestDeploy: true,
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
        .then(() => {
          var reportFile = readFileSync(reportPath, 'utf8');
          if (reportFile === sentinelContent) {
            // Nothing was built, so there's nothing else to do.
            return;
          }
          var report = JSON.parse(reportFile) as IReportFile;
          if (report.modName) {
            activateMod(profile.gameId, report.modName);
          }
        })
        .catch(() => {
          context.api.showErrorNotification(
            'Failed to read report file.',
            `The EasyNPC report file at ${reportPath} either does not exist or could not be opened. ` +
            `If you completed a build, then you may need to restart Vortex in order to see the new mod.`);
        });
    } else {
      context.api.showErrorNotification(
        'EasyNPC not configured',
        'Could not find a registered tool named EasyNPC.exe. Check that this tool is configured in the Vortex dashboard.' +
        JSON.stringify(tools));
    }
  }

  context.registerAction('mod-icons', 998, 'launch-simple', {}, 'Launch EasyNPC', () => launchEasyNpc(), isGameSupported);
  context.registerAction('mod-icons', 999, 'conflict', {}, 'EasyNPC Post-Build', () => launchEasyNpc(['-z']), isGameSupported);
};

function zeroPad(value: number, digits: number) {
  return String(value).padStart(digits, '0');
}

export default init;