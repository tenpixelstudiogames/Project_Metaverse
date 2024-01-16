using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
public class PlayerNetwork : NetworkBehaviour
{
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private Transform spawnedObject;
    private NetworkVariable<int> networkVariable = new NetworkVariable<int>(0);
    private Transform spawnedGameObject;
    private void Awake()
    {
        //networkVariable = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner) ;
    }

    public override void OnNetworkSpawn()
    {
        //networkVariable = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);


        // if(networkVariable.Value==1)
     
        NetworkUIManager.instance.networkVariableText.text = networkVariable.Value.ToString();

        networkVariable.OnValueChanged += (int previousValue, int newValue) =>
        {
            if (IsHost) return;
            Debug.Log(OwnerClientId + "; randomNumber: " + networkVariable.Value);
            NetworkUIManager.instance.networkVariableText.text = networkVariable.Value.ToString();

        };
    }

    // Update is called once per frame
    void Update()
    {
       
        if (!IsOwner) return;
        if(IsHost)
        {
            networkVariable.Value += 1;
            Debug.Log("Updating...");

        }

       // Debug.Log("ID " + OwnerClientId + " value of variable " + networkVariable.Value);
        //Debug.Log("ID " + OwnerClientId);
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // spawnedGameObject = Instantiate(spawnedObject);
           // spawnedGameObject.GetComponent<NetworkObject>().Spawn(true);
            networkVariable.Value = Random.Range(0, 100);
            //TestClientRpc();
        }
        if (Input.GetKeyDown(KeyCode.Backspace))
        {           
            spawnedGameObject.GetComponent<NetworkObject>().Despawn(true);
        }

        Vector3 moveDir = Vector3.zero;

        if (Input.GetKey(KeyCode.W)) moveDir.z = +1f;
        if (Input.GetKey(KeyCode.S)) moveDir.z = -1f;
        if (Input.GetKey(KeyCode.D)) moveDir.x = +1f;
        if (Input.GetKey(KeyCode.A)) moveDir.x = -1f;

        Vector3 normalizedMoveDir = moveDir.normalized;

        transform.position += normalizedMoveDir * moveSpeed * Time.deltaTime;
    }

    [ServerRpc]
    private void TestServerRpc()
    {

            networkVariable.Value = Random.Range(0, 100);    
        Debug.Log(OwnerClientId +" ;TestServerRpc");
    }

    [ClientRpc]
    private void TestClientRpc()
    {
        networkVariable.Value = Random.Range(0, 100);
        Debug.Log(OwnerClientId + " ;TestServerRpc");
    }
}
