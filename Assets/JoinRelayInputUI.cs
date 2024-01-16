using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class JoinRelayInputUI : MonoBehaviour
{
    [SerializeField] private InputField inputField;
   

    private TestingRelay testingRelayScript;
    // Start is called before the first frame update
    void Start()
    {
        testingRelayScript = FindAnyObjectByType<TestingRelay>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void EnterBtnClicked()
    {
        if(!string.IsNullOrEmpty(inputField.text))
        {
            testingRelayScript.JoinRelay(inputField.text);
        }
    }
}
