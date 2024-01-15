using UnityEngine;
using UnityEngine.SceneManagement;

// This class is created for the example scene. There is no support for this script.
public class ControlsTutorial : MonoBehaviour
{
	public GameObject commands;
	private string message = "";
	private bool showMsg = false;

	private int w = 550;
	private int h = 100;
	private Rect textArea;
	private GUIStyle style;
	private Color textColor;
	private int pressCount;

	void Awake()
	{
		style = new GUIStyle();
		style.alignment = TextAnchor.MiddleCenter;
		style.fontSize = 36;
		style.wordWrap = true;
		textColor = Color.white;
		textColor.a = 0;
		textArea = new Rect((Screen.width-w)/2, 0, w, h);

		Application.targetFrameRate = 60;
	}

	void Update()
	{
		commands.SetActive(VirtualInput.GetButton("Submit"));
		EEgg();

		if(VirtualInput.GetButtonDown("Switch"))
		{
			SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex == 0 ? 1 : 0);
		}
	}

	void OnGUI()
	{
		if(showMsg)
		{
			if(textColor.a <= 1)
				textColor.a += 0.5f * Time.deltaTime;
		}
		// no hint to show
		else
		{
			if(textColor.a > 0)
				textColor.a -= 0.5f * Time.deltaTime;
		}

		style.normal.textColor = textColor;

		GUI.Label(textArea, message, style);
	}

	public void SetShowMsg(bool show)
	{
		showMsg = show;
	}

	public void SetMessage(string msg)
	{
		message = msg;
	}

	private void EEgg()
	{
		if(VirtualInput.GetButtonDown("Fly"))
		{
			GameObject player = GameObject.FindGameObjectWithTag("Player");
			if (player.GetComponent<BasicBehaviourMobile>().IsGrounded())
				pressCount = 0;
			else
				pressCount++;

			if(pressCount >= 7 && !player.GetComponent<BasicBehaviourMobile>().IsGrounded())
			{

				player.transform.Find("skeleton/Hips/Spine/Spine1/Spine2/Neck/Head/Head_end/mark").gameObject.SetActive(true);
			}
		}
	}
}
