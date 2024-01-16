using System.Collections.Generic;

// This class represents the basic functions for the virtual Input system, based on touch buttons and analogs.
public static class VirtualInput
{
	// Dictionary that contains all current virtual analogs axis on the scene.
	private static Dictionary<string, VirtualAnalog> analogs = new Dictionary<string, VirtualAnalog>();

	// Dictionary that contains all current virtual buttons on the scene.
	private static Dictionary<string, VirtualButton> buttons = new Dictionary<string, VirtualButton>();

	// Add an analog axis to be managed by the virtual Input.
	public static void AddAxis(string axisName, VirtualAnalog analog)
	{
		if (!analogs.ContainsKey(axisName))
		{
			analogs.Add(axisName, analog);
		}
		else
		{
			analogs[axisName] = analog;
		}
	}

	// Remove an axis for the list being managed by the virtual Input.
	public static bool RemoveAxis(string axisName)
	{
		return analogs.Remove(axisName);
	}

	// Add a button to be managed by the virtual Input.
	public static void AddButton(string buttonName, VirtualButton button)
	{
		if (!buttons.ContainsKey(buttonName))
		{
			buttons.Add(buttonName, button);
		}
		else
		{
			buttons[buttonName] = button;
		}
	}

	// Remove a button for the list being managed by the virtual Input.
	public static bool RemoveButton(string buttonName)
	{
		return buttons.Remove(buttonName);
	}

	// Input  equivalent functions:

	// Returns the value of the virtual axis identified by axisName.
	public static float GetAxis(string axisName)
	{
		if (analogs.ContainsKey(axisName) && analogs[axisName].GetAxis(axisName) != 0)
			return analogs[axisName].GetAxis(axisName);

		return 0;
	}

	// Returns the value of the virtual axis identified by axisName with no smoothing filtering applied.
	public static float GetAxisRaw(string axisName)
	{
		if (analogs.ContainsKey(axisName) && analogs[axisName].GetAxisRaw(axisName) != 0)
		{
			return analogs[axisName].GetAxisRaw(axisName);
		}
		// Check if it's a button.
		else if (buttons.ContainsKey(axisName))
			return buttons[axisName].GetRaw();
		return 0;
	}

	// Returns true while the virtual button identified by buttonName is held down.
	public static bool GetButton(string buttonName)
	{
		if (buttons.ContainsKey(buttonName) && buttons[buttonName].Get())
			return true;
		else if(analogs.ContainsKey(buttonName) && analogs[buttonName].GetButton(buttonName))
			return true;
		return false;
	}

	// Returns true during the frame the user presses the virtual button identified by buttonName.
	public static bool GetButtonDown(string buttonName)
	{
		if (buttons.ContainsKey(buttonName) && buttons[buttonName].GetDown())
			return true;
		return false;
	}

	// Returns true the first frame the user releases the virtual button identified by buttonName.
	public static bool GetButtonUp(string buttonName)
	{
		if (buttons.ContainsKey(buttonName) && buttons[buttonName].GetUp())
			return true;
		return false;
	}

	// Virtual keys works the same way as virtual buttons:
	// Returns true while the user holds down the virtual virtual key identified by name.
	public static bool GetKey(string name)
	{
		if (buttons.ContainsKey(name))
			return buttons[name].Get();
		return false;
	}

	// Returns true during the frame the user starts pressing down the virtual key identified by name.
	public static bool GetKeyDown(string name)
	{
		if (buttons.ContainsKey(name))
			return buttons[name].GetDown();
		return false;
	}

	// Returns true during the frame the user releases the virtual key identified by name.
	public static bool GetKeyUp(string name)
	{
		if (buttons.ContainsKey(name))
			return buttons[name].GetUp();
		return false;
	}

	public static List<string> GetActiveAnalogs()
	{
		var analogIds = new List<string>();
		foreach (KeyValuePair<string, VirtualAnalog> entry in analogs)
		{
			if (entry.Value.GetId >= 0)
			{
				analogIds.Add(entry.Value.GetId.ToString());
			}
		}
		return analogIds;
	}
}
