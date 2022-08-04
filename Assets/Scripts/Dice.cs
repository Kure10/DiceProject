using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Dice : MonoBehaviour
{

    [Space]
    [SerializeField] List<Transform> _sideList = new List<Transform>();

    [SerializeField] MeshRenderer _meshRendered;
    [SerializeField] Material _highlightDiceMaterial;
    [SerializeField] Material _normalDiceMaterial;

    private BoxCollider _boxCollider;
    private Rigidbody _rb;
    private Action<Dice> _onClickAction;
    private int _result = -1;
    private bool _isSelected = false;

    public bool IsSelected
    {
        get
        {
            return _isSelected;
        }
    }

    public bool HasVelocity
    {
        get
        {
            return !(_rb.velocity.x == 0 && _rb.velocity.z == 0 && _rb.velocity.y == 0);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        _boxCollider = this.gameObject.GetComponent<BoxCollider>();
        _rb = this.gameObject.GetComponent<Rigidbody>();
    }

    public int EvaluateThrow()
    {
        Transform topTransform = null;
        float theHighestNumber = 0;

        foreach (var side in _sideList)
        {
            if (theHighestNumber < side.position.y)
            {
                theHighestNumber = side.position.y;
                topTransform = side;
            }
        }

        _result = Int32.Parse(topTransform.name);
        return _result;
    }

    public void RandomThrow(Vector2Int torque, int power)
    {
        if (_rb != null)
        {
            _rb.AddTorque(Random.Range(torque.x, torque.y), Random.Range(torque.x, torque.y), Random.Range(torque.x, torque.y));
            _rb.AddForce(transform.right * power);
        }
    }

    public void DeSelect()
    {
        _isSelected = false;
        _meshRendered.material = _normalDiceMaterial;
        _rb.constraints = RigidbodyConstraints.None;
    }

    public void OnSelectAction()
    {
        _onClickAction?.Invoke(this);
    }

    public void InitDice(Action<Dice> action)
    {
        _onClickAction += action;
    }

    public void SelectDice()
    {
        if (_isSelected == false)
        {
            _isSelected = true;
            _meshRendered.material = _highlightDiceMaterial;
            _rb.constraints = RigidbodyConstraints.FreezeAll;
        }
        else
        {
            _isSelected = false;
            _meshRendered.material = _normalDiceMaterial;
            _rb.constraints = RigidbodyConstraints.None;
        }
    }

    public void FreezeRotation()
    {
        _rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    public void UnFreezeRigidBody()
    {
        _rb.constraints = RigidbodyConstraints.None;
    }


    public void TurnOffPhysics()
    {
        _rb.useGravity = false;
        _boxCollider.enabled = false;
    }

    public void TurnOnPhysics()
    {
        _rb.useGravity = true;
        _boxCollider.enabled = true;
    }
}
