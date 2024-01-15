using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
public class NetworkUIManager : MonoBehaviour
{
    public static NetworkUIManager instance;
    [SerializeField] private Button serverButton;
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;
    public  Text networkVariableText;
    private void Awake()
    {
        instance = this;
        serverButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartServer();
        }
        );

        hostButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartHost();
        }
        );

        clientButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartClient();
        }
        );
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
