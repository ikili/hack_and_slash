using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class PlayerController : AliveObject
{
	public bool isEnemy;
    public bool isAttackEnd;
    public int upgradePoint;
    public float nextLevelXP;
    public MouseTarget mouseTarget;
    [SerializeField]private LayerMask _layerMask;
    [SerializeField]private AliveObject _targetEnemy;
    [SerializeField]private ParticleSystem _particalSystemLevelUp;
    [SerializeField]private float distanceRange;
	[SerializeField]private GameObject[] weapons;
    [SerializeField]private WeaponType currentWeapon;
    enum WeaponType
	{
		Sword,
		Axe,
		Knife
	}
    private NavMeshAgent agent;
    private Animator animator;
    private int amountPointTelent;
    public List<AudioSource> _attackSound;
    public void Start()
    {
        constitution = 125;
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        updateState();
    }
    
    void Update()
    {
        if (hp > 0)
        {
			animator.SetFloat("Speed", agent.velocity.magnitude);
			HideWeapon();
			if (!GamaManager.gm.isStaticPlayer)
            {
				move();
            }
            if (Input.GetMouseButtonUp(0) && isEnemy)
            {
				isAttackEnd = true;        
            }
            else if (Input.GetMouseButton(0) && isEnemy)
            {
                attackAnimationStart();
            }
            passiveSkills();
        }
        if (hp <= 0)
        {
            dead();
        }
    }

    public float getSpeed()
    {
        return agent.velocity.magnitude;
    }
    
    void move()
    {
        if (Input.GetMouseButton(0) && !isEnemy)
        {
            Vector3 mouse = Input.mousePosition;
            Ray ray = Camera.main.ScreenPointToRay(mouse);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                Debug.Log(hit.transform.tag);
                if (hit.transform.gameObject.layer == 10 && Vector3.Distance(hit.transform.position, transform.position) <= distanceRange)
                {
                    AliveObject tmp = hit.transform.gameObject.GetComponent<AliveObject>();
                    if (!tmp.isDead && tmp.isSpawn)
                    {
                        _targetEnemy = tmp;
                        mouseTarget.target = _targetEnemy;
                        mouseTarget.isTarget = true;
                        transform.LookAt(_targetEnemy.gameObject.transform.position);
                        attackAnimationStart();
                    }
                }
                else if (hit.transform.tag == "Weapon" &&
                         Vector3.Distance(hit.transform.position, transform.position) <= distanceRange)
                {
                    PickUpItem(hit.transform.gameObject);
                }
                else if (hit.transform.gameObject.layer != 5)
                {
                    _moveOnPosition(hit.point);
                }
            }
        }
    }

    private void PickUpItem(GameObject toPickup)
    {
        GameObject.Find("InventoryManager").GetComponent<InventoryManager>().TakeItem(toPickup);
    }
    
    private void HideWeapon()
	{
		foreach(var weap in weapons)
		{
			weap.SetActive(false);
		}
		if (animator.GetFloat("Speed") == 0)
			weapons[(int)currentWeapon].SetActive(true);
	}

	public void animationRun(bool status)
    {
        animator.SetBool("Run", status);
    }

    public void hit(float damage)
    {
        hp -= Convert.ToInt32(damage * (1 - armor / 200));
        if (hp < 0)
        {
            hp = 0;
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
	
        if (other.gameObject.tag == "HitPoint")
        {
            increaselifePotion();
            Destroy(other.gameObject);
        }
    }

    public void increaselifePotion()
    {
        hp += Convert.ToInt32(maxHp * 0.3f);
        if (hp > maxHp)
        {
            hp = maxHp;
        }
    }

    public float getDamage()
    {
		WeaponStats weap = weapons[(int)currentWeapon].GetComponent<WeaponStats>();
		return Random.Range(minDamage + weap.damage, maxDamage + weap.damage);
    }

    public int getAmountPointTalent()
    {
        return amountPointTelent;
    }

    public void increasePointTalent()
    {
        amountPointTelent--;
    }
    
    /*
    * For animation event
    */
    public void deadEndAnimation()
    {
        return;
    }
    
    /*
     * For animation event
     */
    public void attack()
    {
        if (isAttackEnd)
        {
            attackAnimationStop();
            mouseTarget.isTarget = false;
            mouseTarget.target = null;
            isAttackEnd = false;
        }

        if (75 + agility - _targetEnemy.agility > 50)
        {
            _targetEnemy.hit(getDamage());
        }
        if (_targetEnemy.hp <= 0 && _targetEnemy.isExp)
        {
            isAttackEnd = true;
            isEnemy = false;
            increaseExp(_targetEnemy.exp);
            _targetEnemy.isExp = false;
        }
    }

    public void beforeAttack()
    {
        _attackSound[Random.Range(0, _attackSound.Count)].Play();
    }
    
    private void _moveOnPosition(Vector3 hit)
    {
        Vector3 difference = hit - transform.position; 
        difference.Normalize();
        float rotationY = Mathf.Atan2(difference.z, difference.x) * Mathf.Rad2Deg;
        agent.transform.rotation = Quaternion.Euler(0f, -rotationY + 90, 0);
        agent.SetDestination(hit);
    }

    public void cheatLevelUp()
    {
        increaseExp(nextLevelXP);
    }
    
    private void increaseExp(float exp)
    {
        this.exp += exp;
        if (this.exp >= nextLevelXP)
        {
            level += 1;
            hp = maxHp;
            upgradePoint += 5;
            amountPointTelent += 1;
            this.exp = this.exp - nextLevelXP;
            double _nextLevelXp = System.Convert.ToDouble(nextLevelXP * 1.5);
            nextLevelXP = System.Convert.ToInt32(Math.Ceiling(_nextLevelXp));
            _particalSystemLevelUp.Play();
        }
    }

    private void attackAnimationStart()
    {
		WeaponStats weap = weapons[(int)currentWeapon].GetComponent<WeaponStats>();
		animator.speed = weap.attackSpeed;
		animator.SetBool("attack", true);
        isEnemy = true;
    }
    
    private void attackAnimationStop()
    {
		animator.speed = 1;
		animator.SetBool("attack", false);
        isEnemy = false;
    }
    
    private void dead()
    {
        animator.SetBool("Dead", true);
    }

    public void ChangeWeapon(WeaponStats weapon)
    {
        currentWeapon = (WeaponType)weapon.weaponType;
    }
}
