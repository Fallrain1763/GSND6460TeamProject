using UnityEngine;

public class Slideshow : MonoBehaviour
{
    [SerializeField] GameObject comic;
    [SerializeField] GameObject[] comicStates;
    int currIdx = 0;

    public void NextSlide()
    {
        currIdx++;
        if (currIdx == comicStates.Length)
        {
            comic.SetActive(false);
        }
        else
        {
            comicStates[currIdx].SetActive(true);
        }
    }
}
