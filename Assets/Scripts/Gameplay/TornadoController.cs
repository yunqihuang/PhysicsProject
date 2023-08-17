using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TornadoController : MonoBehaviour
{
    public Transform tornado;
    public Transform tornadoOmen;
    
    public float lastTime;
    public float waitTime;
    public bool isAlive;
    
    private float _timer;
    // Start is called before the first frame update
    void Start()
    {
        tornado.gameObject.SetActive(false);
        tornadoOmen.gameObject.SetActive(false);
        //_anim = tornado.GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        var time = isAlive ? lastTime : waitTime;
        _timer += Time.deltaTime;
        if (_timer > time)
        {
            _timer = 0;
            isAlive = !isAlive;
            StartCoroutine(PlayTornado());
        }
    }

    void OnDisable()
    {
        tornadoOmen.gameObject.SetActive(false);
        tornado.gameObject.SetActive(false);
    }
    
    IEnumerator PlayTornado()
    {
        if (isAlive)
        {
            tornadoOmen.gameObject.SetActive(isAlive);
            yield return new WaitForSeconds(2.5f);
            tornado.gameObject.SetActive(isAlive);
        }
        else
        {          
            tornado.gameObject.SetActive(isAlive);
            yield return new WaitForSeconds(2.5f);
            tornadoOmen.gameObject.SetActive(isAlive);
        }
    }
}
