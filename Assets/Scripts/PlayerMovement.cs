using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.XR;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private Transform boyeTR, boyeTL, boyeBR, boyeBL;
    [SerializeField] private float rotation_speed, movement_speed;

    [SerializeField] private GameObject model;
    [SerializeField] private Animator animator;
    
    [Header("Sound Stuff")]
    [SerializeField] private AudioClip swimSound;
    [SerializeField] private AudioClip[] quackSounds;
    [SerializeField] private AudioClip[] hitSounds;
    [SerializeField] private float quackCooldownTime = 0.5f;
    
    private ScoreMaster _scoreMaster;
    private AudioSource _swimAudio;
    private AudioSource _quackAudio;
    private AudioSource _hitAudio;
    private float _tQuacked = 0.0f;
    private int _idxPrevQuack = 0;
    private Vector3 _start_pos;
    private Quaternion _start_rot;
    private bool _hittable = true;

    private Transform currentPressedBoye;
    private Dictionary<Transform, float> boyeOriginalYPositions = new Dictionary<Transform, float>();

    void Start()
    {
        CacheBoyeOriginalPositions();
        // get audio components and set them accordingly
        AudioSource[] audioSources = GetComponentsInChildren<AudioSource>();
        _swimAudio = audioSources[0];
        _quackAudio = audioSources[1];
        _hitAudio = audioSources[2];
        _scoreMaster = GameObject.Find("GameMaster").GetComponent<ScoreMaster>();
        _start_pos = transform.position;
        _start_rot = transform.rotation;
        
        _swimAudio.clip = swimSound;
        _tQuacked = Time.time;
    }

    private void CacheBoyeOriginalPositions()
    {
        boyeOriginalYPositions[boyeTL] = boyeTL.position.y + 6;
        boyeOriginalYPositions[boyeTR] = boyeTR.position.y + 6;
        boyeOriginalYPositions[boyeBL] = boyeBL.position.y + 6;
        boyeOriginalYPositions[boyeBR] = boyeBR.position.y + 6;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 target_pos;
        Transform newTarget = null;

        if (Input.GetButton("Quack"))
        {
            Quack();
        }

        if (!_scoreMaster.game_running)
        {
            return;
        }


        
        if (Input.GetButton("TopLeft"))
        {
            target_pos = boyeTL.position - transform.position;
            newTarget = boyeTL;
        }
        else if (Input.GetAxis("JoystickX") > 0.1)
        {
            target_pos = boyeBR.position - transform.position;
            newTarget = boyeBR;
        }
        else if (Input.GetAxis("JoystickX") < -0.1)
        {
            target_pos = boyeTL.position - transform.position;
            newTarget = boyeTL;
        }
        else if (Input.GetAxis("JoystickY") > 0.1)
        {
            target_pos = boyeBL.position - transform.position;
            newTarget = boyeBL;
        }
        else if (Input.GetAxis("JoystickY") < -0.1)
        {
            target_pos = boyeTR.position - transform.position;
            newTarget = boyeTR;
        }
        else if (Input.GetButton("TopRight"))
        {
            target_pos = boyeTR.position - transform.position;
            newTarget = boyeTR;
        }
        else if (Input.GetButton("BottomLeft"))
        {
            target_pos = boyeBL.position - transform.position;
            newTarget = boyeBL;
        }
        else if (Input.GetButton("BottomRight"))
        {
            target_pos = boyeBR.position - transform.position;
            newTarget = boyeBR;
        }
        else
        {
            SwitchTargetBoye(null);
            animator.SetBool("IsMoving", false);
            ToggleAudio(true);
            return;
        }
        SwitchTargetBoye(newTarget);
        animator.SetBool("IsMoving", true);
        ToggleAudio();

        float target_angle = Mathf.Atan2(target_pos.x, target_pos.z) * Mathf.Rad2Deg;
        Quaternion targetRotation = Quaternion.Euler(new Vector3(0, target_angle, 0));
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotation_speed * Time.deltaTime);
        transform.position += movement_speed * Time.deltaTime * transform.forward;
    }

    private void SwitchTargetBoye(Transform newTarget)
    {
        if (newTarget == currentPressedBoye)
        {
            return;
        }

        if (currentPressedBoye != null)
        {
            TweenBoyeToFloating(currentPressedBoye);
        }

        currentPressedBoye = newTarget;

        if (newTarget != null)
        {
            TweenBoyeDown(newTarget);
        }
    }

    private void TweenBoyeToFloating(Transform boye)
    {
        if (boye == null) return;

        boye.DOKill();
        float originalY = boyeOriginalYPositions[boye]; 
        boye.DOMoveY(originalY, 0.5f).SetEase(Ease.InOutSine);
    }

    private void TweenBoyeDown(Transform boye)
    {
        if (boye == null) return;

        boye.DOKill();
        float targetY = boyeOriginalYPositions[boye] - 1f;
        boye.DOMoveY(targetY, 0.3f).SetEase(Ease.OutQuad);
    }

    void OnTriggerEnter(Collider other)
    {
        if (_hittable && other.gameObject.CompareTag("Enemy"))
        {
            _hittable = false;
            if(other.gameObject.name != "Shark")
                Destroy(other.gameObject);
            _scoreMaster.ReduceLive();
            PlayHitAudio();
            StartCoroutine(PlayerBlink());
        }
    }

    private void ToggleAudio(bool stopPlaying = false)
    {
        if (stopPlaying && _swimAudio.isPlaying)
        {
            _swimAudio.Stop();
        }
        else if (!stopPlaying && !_swimAudio.isPlaying)
        {
            _swimAudio.Play();
        }
    }

    public void Reset()
    {
        transform.position = _start_pos;
        transform.rotation = _start_rot;
    }

    private void Quack()
    {
        // check is next quack is available 
        if (Mathf.Abs(_tQuacked - Time.time) < quackCooldownTime)
        {
            return;
        }
        
        // randomization stuff
        int idx_quack = Random.Range(0, quackSounds.Length);
        if (idx_quack == _idxPrevQuack)
        {
            idx_quack = (idx_quack + 1) % quackSounds.Length;
        }
        _quackAudio.clip = quackSounds[idx_quack];

        float r_volume = Random.Range(0.75f, 1.0f);
        _quackAudio.volume = r_volume;
        
        float r_pitch = Random.Range(0.5f, 1.5f);
        _quackAudio.pitch = r_pitch;
        
        float r_nextQuackTime = Random.Range(0.3f, 1.0f);
        quackCooldownTime = r_nextQuackTime;
        
        // ------------------------
        _quackAudio.Play();
        
        // set tracking stuff
        _idxPrevQuack = idx_quack;
        _tQuacked = Time.time;
    }

    private void PlayHitAudio()
    {
        int idx_hit = Random.Range(0, hitSounds.Length);
        _hitAudio.clip = hitSounds[idx_hit];
        _hitAudio.Play();
    }

    IEnumerator PlayerBlink()
    {
        for (int i = 0; i < 3; i++)
        {
            model.SetActive(false);
            yield return new WaitForSeconds(0.2f);
            model.SetActive(true);
            yield return new WaitForSeconds(0.2f);
        }

        _hittable = true;
    }
}
