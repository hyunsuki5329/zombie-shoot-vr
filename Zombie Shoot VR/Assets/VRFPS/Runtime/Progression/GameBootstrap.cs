using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace VRFPS
{
    public sealed class GameBootstrap : MonoBehaviour
    {
        const string Combat="Assets/VRFPS/Scenes/Combat.unity";
        Scene ownedCombat;
        bool starting,shuttingDown;
        public bool IsStarting => starting;
        public string RecordDirectoryOverride { get; set; }
        public bool IsReady { get; private set; }
        public string LastError { get; private set; }
        public CombatSceneEntry Entry { get; private set; }
        IEnumerator Start()
        {
            if(shuttingDown)yield break;
            starting=true;
            try
            {
                AsyncOperation operation=null;
                try
                {
                    if(SceneManager.GetSceneByPath(Combat).isLoaded)throw new InvalidOperationException("A combat scene is already active.");
                    if(!Application.CanStreamedLevelBeLoaded(Combat))throw new InvalidOperationException("Combat scene is missing from this build.");
                    operation=SceneManager.LoadSceneAsync(Combat,LoadSceneMode.Additive);
                }
                catch(Exception error){LastError=error.Message;}
                if(operation==null)yield break;
                yield return operation;ownedCombat=SceneManager.GetSceneByPath(Combat);
                if(shuttingDown)yield break;
                try
                {
                    Entry=ownedCombat.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<CombatSceneEntry>(true)).Single();
                    Entry.Initialize(new RecordStore(RecordDirectoryOverride ?? Application.persistentDataPath));
                    IsReady=true;
                }
                catch(Exception error){LastError=error.Message;}
            }
            finally{starting=false;}
        }
        public IEnumerator Shutdown()
        {
            shuttingDown=true;IsReady=false;
            while(starting)yield return null;
            if(Entry!=null)yield return Entry.Shutdown();
            Entry=null;
            if(ownedCombat.IsValid() && ownedCombat.isLoaded)yield return SceneManager.UnloadSceneAsync(ownedCombat);
            ownedCombat=default;
        }
    }
}
