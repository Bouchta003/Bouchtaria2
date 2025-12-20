using System;
using UnityEngine;

public class Card : MonoBehaviour
{
    Collider2D col;
    Vector3 startDragPosition;

    void Start()
    {
        col = GetComponent<Collider2D>();
    }

    private void OnMouseDown()
    {
        startDragPosition = transform.position;
        transform.position = GetMousePositionInWorldSpace();
    }
    private void OnMouseDrag()
    {
        transform.position = GetMousePositionInWorldSpace();
    }

    private Vector3 GetMousePositionInWorldSpace()
    {
        Vector3 p = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        p.z = 0f;
        return p;
    }

    private void OnMouseUp()
    {
        col.enabled = false;
        Collider2D hitCollider = Physics2D.OverlapPoint(transform.position);
        col.enabled = true;
        if(hitCollider != null && hitCollider.TryGetComponent(out ICardDropArea cardDropArea))
        {
            cardDropArea.OnCardDrop(this);
        }else
        {
            transform.position = startDragPosition;
        }
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
