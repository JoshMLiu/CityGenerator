using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Information : MonoBehaviour {

	MeshRenderer renderer;
	public int subdivisionsx = 40; 
	public int areasperstat = 8;
	public int landiterations = 0;

	public float landpercentage = 0.5f;
	public float waterpercentage = 0.5f;

	public string mapname = "NewYork";

	void Start(){
		renderer = (MeshRenderer) transform.GetComponent<MeshRenderer> ();
		renderer.material.color = Color.white;
		Cursor.visible = true;
	}

	void OnMouseEnter(){
		renderer.material.color = Color.red;
	}

	void OnMouseExit() {
		renderer.material.color = Color.white;
	}

	void OnMouseUp(){

		// Get information from input fields
		GameObject inputs = GameObject.Find ("Canvas");
		foreach (Transform child in inputs.transform) {

			if (child.name.Equals ("SubX")) {
				InputField field = (InputField)child.GetComponent<InputField> ();
				string val = field.text;
				int intval = int.Parse (val);
				subdivisionsx = intval;
			} else if (child.name.Equals ("StatArea")) {
				InputField field = (InputField)child.GetComponent<InputField> ();
				string val = field.text;
				int intval = int.Parse (val);
				areasperstat = intval;
			} else if (child.name.Equals ("Landiterations")) {
				InputField field = (InputField)child.GetComponent<InputField> ();
				string val = field.text;
				int intval = int.Parse (val);
				landiterations = intval;
			} else if (child.name.Equals ("landpercentage")) {
				InputField field = (InputField)child.GetComponent<InputField> ();
				string val = field.text;
				float fval = float.Parse (val);
				landpercentage = fval;
			} else if (child.name.Equals ("waterpercentage")) {
				InputField field = (InputField)child.GetComponent<InputField> ();
				string val = field.text;
				float fval = float.Parse (val);
				waterpercentage = fval;
			}  else if (child.name.Equals ("Dropdown")) {
				Dropdown dd = (Dropdown)child.GetComponent<Dropdown> ();
				int menuIndex = dd.value;
				List<Dropdown.OptionData> menuOptions = dd.options;
				string city = menuOptions [menuIndex].text;
				mapname = city;
			}

		}
		// Make object persist to main scene
		GameObject.DontDestroyOnLoad (this);

		Application.LoadLevel("Main");

	} 

}