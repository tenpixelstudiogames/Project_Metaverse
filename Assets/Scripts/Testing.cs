using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class Testing : MonoBehaviour
{
    bool a = false;
    // Start is called before the first frame update
  async void Start()
    {
        var temp = await TestTask();
       // StartCoroutine(Delay());
        Debug.Log("1");
    }

    // Update is called once per frame
    void Update()
    {
        
    }



    private async Task<string> TestTask()
    {
      
        //  await Task.Delay(3000);
        StartCoroutine(Delay(a));
      while(!a)
        {
            Debug.Log(".");
           await Task.Yield();
        }
        Debug.Log("2");
        return null;
    }

    IEnumerator Delay(bool a)
    {
        yield return new WaitForSeconds(3);
        this.a = true;
    }
}
