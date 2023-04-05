using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using SimpleFileBrowser;
using System;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
	public GameObject UIPanelLoading;
	public Text UITextPath;
	public Text UIEventLog;
	string vwf_path;

	// Start is called before the first frame update
	void Start()
	{
		//Init UI
		UIPanelLoading.SetActive(false);
		UIEventLog.text = PlayerPrefs.GetString("EventLog");
		UITextPath.text = PlayerPrefs.GetString("PathVwf");

		// Set plguin VWF path: Check if in BUILD (.exe) or in DEV (Unity Editor)
		if (Application.platform == RuntimePlatform.WindowsEditor)
			 vwf_path = Application.dataPath + @"\Plugins\VIOSO\vioso.vwf"; 
		else vwf_path = Application.dataPath + @"\Plugins\x86_64\vioso.vwf";

		//Set File Browser Filters
		FileBrowser.SetFilters(true, new FileBrowser.Filter("Calib", ".vwf"));
		FileBrowser.SetDefaultFilter(".vwf");
	}

	//Called by Load Button 
	public void ButtonSelect()
	{
		FileBrowser.ShowLoadDialog((path) => { SelectVwf(path); }, null, FileBrowser.PickMode.Files, false, null,null, "Select VWF", "Load");
	}

	//  File Selected method
    void SelectVwf(string[] path)
    {
		Debug.Log("VWF loaded: "+ FileBrowser.Result[0]);
		UITextPath.text = Path.GetFileName(FileBrowser.Result[0]);
		PlayerPrefs.SetString("PathVwf",UITextPath.text);

		//Load the VWF selected 
        StartCoroutine(ApplyVWF());
    }


	//Called by Reset Button 
	public void ButtonReset()
	{
		PlayerPrefs.SetString("EventLog", "\n Scene Reloaded");
		SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

	}

	// Coroutine to make sure loading screen (UI) starts before operations
	IEnumerator ApplyVWF()
    {
		UIPanelLoading.SetActive(true);
		yield return new WaitForSeconds(0.5f);
		//Copy vwf to plugin path
		File.Copy(FileBrowser.Result[0], vwf_path, true);
		PlayerPrefs.SetString("EventLog", "Successfully Loaded: " + FileBrowser.Result[0]);
		SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
	}

	//Application Exit event: cleanup
    private void OnApplicationQuit()
    {
		PlayerPrefs.DeleteAll();
	}
}
