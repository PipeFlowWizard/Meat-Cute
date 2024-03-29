﻿using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

public class GameManager : MonoBehaviour
{
    private static GameManager _instance;

    public static GameManager Instance
    {
        get => _instance;
    }

    static public GameObject[] selected = new GameObject[2];
    public int score = 0;
    public TextMeshProUGUI populationText, scoreText, timeText, endScoreText;
    public GameObject matchParticleBurst;
    public CinemachineVirtualCamera cam;
    private CinemachineImpulseSource impulse;
    public Material defaultSpriteMaterial, selectedMaterial;
    public RecipeDisplay RecipeDisplay;
    public TextPopup textPopup;
    public string[] flavourText;

    public int blobPopulation = 0;
    public int maxBlobPopulation = 10;
    public int numAppendages = 0;
    public Recipe recipe;
    private float remainingTime = 30f;
    public float timeLimit = 120;
    



    /// <summary>
    /// Instantiate singleton
    /// </summary>
    private void Awake()
    {
        
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
        }
    }

    void Start()
    {
        textPopup = GetComponent<TextPopup>();
        recipe = new Recipe();
        recipe.GenerateRecipe();
        RecipeDisplay.UpdateRecipe(recipe.NumLeftArms,recipe.NumRightArms,recipe.NumLeftLegs, recipe.NumRightLegs);
        impulse = GetComponent<CinemachineImpulseSource>();
        score = 0;
        remainingTime = timeLimit;
        DisplayRemainingTime();
        UpdateScoreDisplay();
        UpdatePopulationDisplay();
    }

    private void FixedUpdate()
    {
        remainingTime -= Time.deltaTime;
        DisplayRemainingTime();
        if (remainingTime < 0)
            EndGame();
    }

    private void DisplayRemainingTime()
    {
        if (!timeText)
            return;
        timeText.text = (int)remainingTime / 60 + " : " + (remainingTime % 60).ToString("0.00");
    }
    

    public void AddScore(int amount)
    {
        score += amount;
        UpdateScoreDisplay();
    }


    private void UpdateScoreDisplay()
    {
        if (!scoreText)
            return;
        scoreText.text = "" + score;
    }

    public void UpdatePopulationDisplay()
    {
        populationText.text = "" + blobPopulation;
    }

    private void EndGame()
    {
        Time.timeScale = 0;
        endScoreText.text = "Final Score :" + score;
        endScoreText.transform.root.gameObject.SetActive(true);
    }



    /// <summary>
    /// Called by the arrow behaviour when an arrow hits an object
    /// </summary>
    /// <param name="gameObject">The gameobject hit by the arrow</param>
    public void AssignSelection(GameObject gameObject)
    {
        // Get the top most object in the gameobject tree
        var mainObject = gameObject.transform.root.gameObject;
        var spawner = FindObjectOfType<Spawner>();

        if (mainObject.CompareTag("Appendage") && (mainObject.GetComponent<Appendage>().isSocketed ||
                                                   mainObject.GetComponent<Appendage>().socket.HasAppendage))
        {
            Debug.Log("This appendage is already attached to something");
            return;
        }

        // Ensure that the object is an appendage or a blob
        if (mainObject.CompareTag("Appendage") || mainObject.CompareTag("Blob"))
        {
            if (selected[0] == null)
            {
                selected[0] = mainObject;
                SetSpriteRendererMaterialInAllChildren(mainObject.transform, selectedMaterial);
                FindObjectOfType<DisplaySelectedItem>().UpdateDisplay(mainObject);
            }
            else if (selected[1] == null && (selected[0] != mainObject))
            {
                selected[1] = mainObject;
            }

            // If two objects are selected, try to attach them depending on the item combo
            if (selected[0] != null && selected[1] != null)
            {

                //Case 1: Blob x Blob
                if (selected[0].CompareTag("Blob") && selected[1].CompareTag("Blob"))
                {
                    // Pair them together
                    MatchBlobs(selected[0].GetComponent<Blob>(), selected[1].GetComponent<Blob>());
                }

                //Case 2: Appendage x Appendage
                if (selected[0].CompareTag("Appendage") && selected[1].CompareTag("Appendage"))
                {
                    selected[0].GetComponent<Appendage>().AttachToMe(selected[1].GetComponent<Appendage>());
                    AddScore(-20);
                    textPopup.Create(selected[1].transform.position, "-20",Color.red);
                    textPopup.Create(selected[1].transform.position, "Limbs Wasted!");
                    for(int i = 0; i < UnityEngine.Random.Range(1,3); i++)
                    {
                        spawner.SpawnAppendage();
                    }
                }

                //Case 3: Blob x Appendage || Appendage x Blob
                if (selected[0].CompareTag("Blob") && selected[1].CompareTag("Appendage"))
                {
                    selected[0].GetComponent<Blob>().Attach(selected[1].GetComponent<Appendage>());
                    AddScore(10);
                    textPopup.Create(selected[1].transform.position, "+10",Color.green);
                    if(flavourText != null)
                        textPopup.Create(selected[1].transform.position, flavourText[UnityEngine.Random.Range(0,flavourText.Length)]);

                }

                if (selected[0].CompareTag("Appendage") && selected[1].CompareTag("Blob"))
                {
                    selected[1].GetComponent<Blob>().Attach(selected[0].GetComponent<Appendage>());
                    AddScore(10);
                    textPopup.Create(selected[1].transform.position, "+10",Color.green);
                    if(flavourText != null)
                        textPopup.Create(selected[1].transform.position, flavourText[UnityEngine.Random.Range(0,flavourText.Length)]);
                }

                SetSpriteRendererMaterialInAllChildren(selected[0].transform, defaultSpriteMaterial);
                FindObjectOfType<DisplaySelectedItem>().UpdateDisplay(null);
                // Empty the selected items array
                Array.Clear(selected, 0, 2);
                
            }

        }

    }

    /// <summary>
    /// Matches two blobs together
    /// </summary>
    /// <param name="other">Blob to be matched with</param>
    public void MatchBlobs(Blob blob1, Blob blob2)
    {
        var spawner = FindObjectOfType<Spawner>();
        var totalNumLegs = blob1.NumLegs + blob2.NumLegs;
        var totalNumArms = blob1.NumArms + blob2.NumArms;
        var totalNumLeftArms = blob1.numLeftArms + blob2.numLeftArms;
        var totalNumRightArms = blob1.numRightArms + blob2.numRightArms;
        var totalNumLeftLegs = blob1.numLeftLegs + blob2.numLeftLegs;
        var totalNumRightLegs = blob1.numRightLegs + blob2.numRightLegs;
        
        int points = 0;

        // Criteria for now
        // if blobs have nothing on them, - 5 points
        if (totalNumLegs == 0 && totalNumArms == 0)
        {
            points = -50;
            textPopup.Create(selected[1].transform.position, "Meh...");
        }
        else
        {
            points += totalNumArms * 10;
            points += totalNumLegs * 10;
        
            if (totalNumLeftArms == totalNumRightArms)
            {
                textPopup.Create(selected[1].transform.position, "Matching Arms!");
                points += totalNumArms * 10;
            }
            if (totalNumLeftLegs == totalNumRightLegs)
            {
                textPopup.Create(selected[1].transform.position, "Matching Legs!");
                points += totalNumLegs * 10;
            }

            GetComponent<AudioSource>().Play();
        }

        points += CheckIfSatisfyRecipe(blob1) + CheckIfSatisfyRecipe(blob2);
        var color = points >= 0 ? Color.green : Color.red;
        textPopup.Create(selected[1].transform.position, "+" + points.ToString(), color);
        AddScore(points);
        
        // Play particles
        impulse.GenerateImpulse();
        Instantiate(matchParticleBurst, blob1.transform.position, quaternion.identity);
        Instantiate(matchParticleBurst, blob2.transform.position, quaternion.identity);
        
        blobPopulation -= 2;
        UpdatePopulationDisplay();

        Destroy(blob1.gameObject);
        Destroy(blob2.gameObject);
        
        for(int i = 0; i < UnityEngine.Random.Range(1,3); i++)
            spawner.SpawnBlob(UnityEngine.Random.Range(0, 4));
        for(int i = 0; i < UnityEngine.Random.Range(0, 4); i++)
        {
            spawner.SpawnAppendage();
        }

    }
    

    private void SetSpriteRendererMaterialInAllChildren(Transform t, Material mat)
    {
        SpriteRenderer rend = t.GetComponent<SpriteRenderer>();
        if (rend)
            rend.material = mat;

        foreach(Transform child in t)
        {
            SetSpriteRendererMaterialInAllChildren(child, mat);
        }
    }

    /// <summary>
    /// Checks the degree to which a blob conforms to a recipe
    /// If it doesn't match at all, return 0
    /// </summary>
    /// <param name="blob">the blob to be checked</param>
    /// <returns></returns>
    public int CheckIfSatisfyRecipe(Blob blob)
    {
        int points = 0;
        bool perfect = false;
        // If there is a basic match
        if (blob.NumArms == recipe.NumArms && blob.NumLegs == recipe.NumLegs)
        {
            var bonusTime = 5f;
            points += 200;
            // If there is a perfect match
            if (blob.numLeftArms == recipe.NumLeftArms && blob.numRightArms == recipe.NumRightArms &&
                blob.numLeftLegs == recipe.NumLeftLegs && blob.numRightLegs == recipe.NumRightLegs)
            {
                bonusTime += 10f;   
                points += 200;
                perfect = true;
            }

            remainingTime += bonusTime;
            textPopup.Create(blob.transform.position, "+" + bonusTime + "s",Color.yellow);
            recipe.GenerateRecipe();
            RecipeDisplay.UpdateRecipe(recipe.NumLeftArms,recipe.NumRightArms,recipe.NumLeftLegs, recipe.NumRightLegs);
            if(perfect)
                textPopup.Create(blob.transform.position, "Perfect Match!",Color.yellow);
            else textPopup.Create(blob.transform.position, "Match",Color.yellow);
        }
        

        return points;
    }
    
    
}


