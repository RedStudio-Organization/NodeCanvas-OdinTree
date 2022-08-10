using NodeCanvas.DialogueTrees;
using NodeCanvas.Framework;
using ParadoxNotion;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.EditorCoroutines.Editor;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;

[System.Serializable]
public class LocalizedStatement : IStatement
{
    public LocalizedString LocalizedText;
    [ReadOnly] public string _text;
    public AudioClip _audio;
    public string _meta;

    EditorCoroutine _editorRoutine;
    [Button("Update Node")]
    bool InternalUpdateDialogue()
    {
        if (_editorRoutine != null) return true;
        _editorRoutine = EditorCoroutineUtility.StartCoroutine(ImportRoutine(), this);
        IEnumerator ImportRoutine()
        {
            LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.GetLocale(new LocaleIdentifier("fr"));
            yield return LocalizationSettings.InitializationOperation;  // Wait localization service
            AsyncOperationHandle<StringTable> asyncOperationHandle =
                LocalizationSettings.StringDatabase
                .GetTableAsync(LocalizedText.TableReference.TableCollectionName, LocalizationSettings.AvailableLocales.GetLocale(new LocaleIdentifier("fr")));
            yield return asyncOperationHandle;
            if (asyncOperationHandle.Status == AsyncOperationStatus.Failed)
            {
                Debug.Log("Error");
                yield break;
            }
            _text = asyncOperationHandle.Result.First(i=>i.Key == LocalizedText.TableEntryReference.KeyId).Value.Value;
            _editorRoutine = null;
        }
        return true;
    }

    //required
    public LocalizedStatement() { }

    string IStatement.text => LocalizedText.GetLocalizedString();
    AudioClip IStatement.audio => _audio;
    string IStatement.meta => _meta;

    ///<summary>Replace the text of the statement found in brackets, with blackboard variables ToString and returns a Statement copy</summary>
    public IStatement BlackboardReplace(IBlackboard bb)
    {
        var copy = ParadoxNotion.Serialization.JSONSerializer.Clone<LocalizedStatement>(this);

        copy._text = copy._text.ReplaceWithin('[', ']', (input) =>
        {
            object o = null;
            if (bb != null)
            { //referenced blackboard replace
                var v = bb.GetVariable(input, typeof(object));
                if (v != null) { o = v.value; }
            }

            if (input.Contains("/"))
            { //global blackboard replace
                var globalBB = GlobalBlackboard.Find(input.Split('/').First());
                if (globalBB != null)
                {
                    var v = globalBB.GetVariable(input.Split('/').Last(), typeof(object));
                    if (v != null) { o = v.value; }
                }
            }
            return o != null ? o.ToString() : input;
        });

        return copy;
    }

    public override string ToString()
    {
        return _text;
    }

}
