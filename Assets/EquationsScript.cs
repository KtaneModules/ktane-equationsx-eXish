using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using System.Text.RegularExpressions;

public class EquationsScript : MonoBehaviour {

    public KMAudio audio;
    public KMBombInfo bomb;

    public KMSelectable[] buttons;

    public GameObject inputdisplay;
    public GameObject symboldisplay;
    public GameObject numbersdisplay;

    private int[] symbols = {0,1,2,3,4,5,6,7,8};
    int symbol;

    private string numbers;
    private string[] cheers = {"Nice!","GJ","Defused","Cleared","We did it?"};

    private double answerlong;
    private double answersimp;
    //moved here to allow for rule 3 of position to function properly
    private float[] newnumbers;

    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;
    private bool typeNothing;
    private bool typeSomething;
    private int announceonce = 0;

    void Awake()
    {
        moduleId = moduleIdCounter++;
        moduleSolved = false;
        foreach(KMSelectable obj in buttons){
            KMSelectable pressed = obj;
            pressed.OnInteract += delegate () { PressButton(pressed); return false; };
        }
    }

    void Start () {
        symbol = Random.RandomRange(0, 9);
        numbers = DigitToNumbers(symbol);
        inputdisplay.GetComponentInChildren<TextMesh>().text = "";
        symboldisplay.GetComponentInChildren<TextMesh>().text = DigitToSymbol(symbol);
        numbersdisplay.GetComponentInChildren<TextMesh>().text = numbers;
        
        answerlong = calculateAnswer(numbers);
        answersimp = System.Math.Round(answerlong, 0, System.MidpointRounding.AwayFromZero);
        answersimp = System.Math.Abs(answersimp);
        if(typeNothing != true)
        {
            Debug.LogFormat("[Equations X #{0}] Answer to Equations X #{0} (unsimplified): {1}", moduleId, answerlong);
            Debug.LogFormat("[Equations X #{0}] Answer to Equations X #{0}: {1}", moduleId, answersimp);
        }
    }
	
	void Update () {
        if ((bomb.GetSolvedModuleNames().Count >= 2) && (symbol == 5))
        {
            if (announceonce != 2)
            {
                announceonce = 1;
                bool rule1 = false;
                bool rule2 = false;
                bool rule3 = false;
                bool rule4 = false;
                bool rule5 = false;
                if ((bomb.GetBatteryCount() > 1) && isAnyPlateEmpty())
                {
                    rule1 = true;
                }
                if (bomb.GetSolvedModuleNames().Count >= 2)
                {
                    rule2 = true;
                }
                if (bomb.IsIndicatorOn("FRQ"))
                {
                    rule3 = true;
                }
                if (!bomb.IsIndicatorOn("FRQ"))
                {
                    if ((bomb.GetModuleNames().Count - bomb.GetSolvableModuleNames().Count) > 0)
                    {
                        rule4 = true;
                    }
                    if (bomb.IsIndicatorOff("BOB"))
                    {
                        rule5 = true;
                    }
                }
                answerlong = reCalcTorque(rule1, rule2, rule3, rule4, rule5, newnumbers[0], newnumbers[1]);
                answersimp = System.Math.Round(answerlong, 0, System.MidpointRounding.AwayFromZero);
                answersimp = System.Math.Abs(answersimp);
                Debug.LogFormat("[Equations X #{0}] New answer to Equations X #{0} (unsimplified): {1}", moduleId, answerlong);
                Debug.LogFormat("[Equations X #{0}] New answer to Equations X #{0}: {1}", moduleId, answersimp);
            }
        }
        //here to prevent from happening infinitely
        if ((announceonce == 1) && (symbol == 5))
        {
            announceonce = 2;
        }
        if ((bomb.GetSolvedModuleNames().Count >= 1) && (symbol == 2))
        {
            if (announceonce != 2)
            {
                announceonce = 1;
                bool rule1 = false;
                if (bomb.GetSerialNumber().Contains('3') || bomb.GetSerialNumber().Contains('5'))
                {
                    rule1 = true;
                }
                answerlong = reCalcPosition(rule1, newnumbers[0], newnumbers[1], newnumbers[2], newnumbers[3]);
                answersimp = System.Math.Round(answerlong, 0, System.MidpointRounding.AwayFromZero);
                answersimp = System.Math.Abs(answersimp);
                Debug.LogFormat("[Equations X #{0}] New answer to Equations X #{0} (unsimplified): {1}", moduleId, answerlong);
                Debug.LogFormat("[Equations X #{0}] New answer to Equations X #{0}: {1}", moduleId, answersimp);
            }
        }
        //here to prevent from happening infinitely
        if ((announceonce == 1) && (symbol == 2))
        {
            announceonce = 2;
        }
        if ((bomb.GetStrikes() == 2) && (symbol == 7))
        {
            typeNothing = true;
            if(announceonce != 2)
            {
                announceonce = 1;
            }
        }
        //here to prevent from happening infinitely
        if ((announceonce == 1) && (symbol == 7))
        {
            Debug.LogFormat("[Equations X #{0}] Rule 1 is now {1}!", moduleId, typeNothing);
            Debug.LogFormat("[Equations X #{0}] Answer is now to press submit with no input!", moduleId);
            announceonce = 2;
        }
	}

    void PressButton(KMSelectable pressed)
    {
        if(moduleSolved != true)
        {
            pressed.AddInteractionPunch(0.5f);
            if (pressed.GetComponentInChildren<TextMesh>().text.Equals("C"))
            {
                audio.PlaySoundAtTransform("buttonClickCustom", transform);
                inputdisplay.GetComponentInChildren<TextMesh>().text = "";
            }
            else if (pressed.GetComponentInChildren<TextMesh>().text.Equals("SUBMIT"))
            {
                audio.PlaySoundAtTransform("submitButton", transform);
                int checkanswer = 0;
                int.TryParse(inputdisplay.GetComponentInChildren<TextMesh>().text, out checkanswer);
                if ((typeNothing == true) && (inputdisplay.GetComponentInChildren<TextMesh>().text.Length == 0))
                {
                    Debug.LogFormat("[Equations X #{0}] You entered nothing, nothing was expected", moduleId, checkanswer);
                    Debug.LogFormat("[Equations X #{0}] Nothing was correct, good job!", moduleId);
                    GetComponent<KMBombModule>().HandlePass();
                    moduleSolved = true;
                    inputdisplay.GetComponentInChildren<TextMesh>().text = randomCheer();
                }
                else if ((typeNothing == true) && !(inputdisplay.GetComponentInChildren<TextMesh>().text.Length == 0))
                {
                    Debug.LogFormat("[Equations X #{0}] You entered {1}, nothing was expected", moduleId, checkanswer);
                    Debug.LogFormat("[Equations X #{0}] {1} was incorrect, try again!", moduleId, checkanswer);
                    GetComponent<KMBombModule>().HandleStrike();
                }
                else if ((typeSomething == true) && inputdisplay.GetComponentInChildren<TextMesh>().text.Equals("116"))
                {
                    Debug.LogFormat("[Equations X #{0}] You entered 116, 116 was expected", moduleId);
                    Debug.LogFormat("[Equations X #{0}] 116 was correct, good job!", moduleId);
                    GetComponent<KMBombModule>().HandlePass();
                    moduleSolved = true;
                    inputdisplay.GetComponentInChildren<TextMesh>().text = randomCheer();
                }
                else if ((typeSomething == true) && !inputdisplay.GetComponentInChildren<TextMesh>().text.Equals("116"))
                {
                    Debug.LogFormat("[Equations X #{0}] You entered {1}, 116 was expected", moduleId, checkanswer);
                    Debug.LogFormat("[Equations X #{0}] {1} was incorrect, try again!", moduleId, checkanswer);
                    GetComponent<KMBombModule>().HandleStrike();
                }
                else if (checkanswer == answersimp)
                {
                    Debug.LogFormat("[Equations X #{0}] You entered {1}, {2} was expected", moduleId, checkanswer, answersimp);
                    Debug.LogFormat("[Equations X #{0}] {1} was correct, good job!", moduleId, checkanswer);
                    GetComponent<KMBombModule>().HandlePass();
                    moduleSolved = true;
                    inputdisplay.GetComponentInChildren<TextMesh>().text = randomCheer();
                }
                else
                {
                    Debug.LogFormat("[Equations X #{0}] You entered {1}, {2} was expected", moduleId, checkanswer, answersimp);
                    Debug.LogFormat("[Equations X #{0}] {1} was incorrect, try again!", moduleId, checkanswer);
                    GetComponent<KMBombModule>().HandleStrike();
                }
            }
            else if (inputdisplay.GetComponentInChildren<TextMesh>().text.Length <= 6)
            {
                audio.PlaySoundAtTransform("buttonClickCustom", transform);
                inputdisplay.GetComponentInChildren<TextMesh>().text += pressed.GetComponentInChildren<TextMesh>().text;
            }
        }
    }

    private string DigitToSymbol(int digit)
    {
        if(digit == 0)
        {
            return "H(T)";
        }else if (digit == 1)
        {
            return "R";
        }else if (digit == 2)
        {
            return "\u03C7";
        }else if (digit == 3)
        {
            return "w";
        }else if (digit == 4)
        {
            return "Z(T)";
        }else if (digit == 5)
        {
            return "t";
        }else if (digit == 6)
        {
            return "m";
        }else if (digit == 7)
        {
            return "a";
        }else
        {
            return "K";
        }
    }

    private string DigitToNumbers(int digit)
    {
        if (digit == 0)
        {
            return ""+Random.RandomRange(0, 100)+"."+Random.RandomRange(0, 100);
        }
        else if (digit == 1)
        {
            return "" + Random.RandomRange(0, 100) + "." + Random.RandomRange(0, 100);
        }
        else if (digit == 2)
        {
            return "" + Random.RandomRange(0, 100) + "." + Random.RandomRange(0, 100) + "." + Random.RandomRange(0, 100) + "." + Random.RandomRange(0, 100);
        }
        else if (digit == 3)
        {
            return "" + Random.RandomRange(0, 100) + "." + Random.RandomRange(0, 100);
        }
        else if (digit == 4)
        {
            return "" + Random.RandomRange(0, 100) + "." + Random.RandomRange(0, 100);
        }
        else if (digit == 5)
        {
            return "" + Random.RandomRange(0, 100) + "." + Random.RandomRange(0, 100);
        }
        else if (digit == 6)
        {
            return "" + Random.RandomRange(0, 100) + "." + Random.RandomRange(0, 100);
        }
        else if (digit == 7)
        {
            return "" + Random.RandomRange(0, 100) + "." + Random.RandomRange(0, 100) + "." + Random.RandomRange(0, 100);
        }
        else
        {
            return "" + Random.RandomRange(0, 100) + "." + Random.RandomRange(0, 100);
        }
    }

    private float calculateAnswer(string numbers)
    {
        float tempanswer = -1;
        if (symbol == 2)
        {
            string[] numssplit = numbers.Split('.');
            newnumbers = new float[4];
            int counter = 0;
            foreach(string num in numssplit)
            {
                float.TryParse(numssplit[counter], out newnumbers[counter]);
                counter++;
            }

            Debug.LogFormat("[Equations X #{0}] The symbol is Position", moduleId);
            Debug.LogFormat("[Equations X #{0}] The numbers are {1}, {2}, {3}, and {4}", moduleId, newnumbers[0], newnumbers[1], newnumbers[2], newnumbers[3]);
            Debug.LogFormat("[Equations X #{0}] The initial equation is: num1 * cos((num2 * num3) + num4)", moduleId);
            //Start Position Rules
            bool rule1 = false;
            bool rule2 = false;
            bool rule3 = false;
            Debug.LogFormat("[Equations X #{0}] Rule checks for Position...", moduleId);
            if (bomb.GetSerialNumber().Contains('3') || bomb.GetSerialNumber().Contains('5'))
            {
                rule1 = true;
            }
            Debug.LogFormat("[Equations X #{0}] Rule 1: {1}", moduleId, rule1);
            if (bombHasModule("Keypad"))
            {
                if (numbers.Contains('5'))
                {
                    numbers = numbers.Replace('5', '8');
                    string[] numssplit2 = numbers.Split('.');
                    for (int i = 0; i < newnumbers.Length; i++)
                    {
                        float temp = 0;
                        float.TryParse(numssplit2[i], out temp);
                        newnumbers[i] = temp;
                    }
                }
                rule2 = true;
            }
            Debug.LogFormat("[Equations X #{0}] Rule 2: {1}", moduleId, rule2);
            if (bomb.GetSolvedModuleNames().Count >= 1)
            {
                rule3 = true;
            }
            Debug.LogFormat("[Equations X #{0}] Rule 3: {1}", moduleId, rule3);
            if ((rule1 == false) && (rule3 == false))
            {
                tempanswer = newnumbers[0] * Mathf.Cos(((newnumbers[1] * newnumbers[2]) + newnumbers[3]) * ((Mathf.PI) / 180));
                Debug.LogFormat("[Equations X #{0}] New Equation: num1 * cos((num2 * num3) + num4)", moduleId);
                Debug.LogFormat("[Equations X #{0}] With number substitutions: {1} * cos(({2} * {3}) + {4})", moduleId, newnumbers[0], newnumbers[1], newnumbers[2], newnumbers[3]);
            }
            else if ((rule1 == true) && (rule3 == true))
            {
                tempanswer = (newnumbers[0] * Mathf.Sin(((newnumbers[1] * newnumbers[2]) + newnumbers[3]) * ((Mathf.PI) / 180))) + 21;
                Debug.LogFormat("[Equations X #{0}] New Equation: (num1 * sin((num2 * num3) + num4)) + 21", moduleId);
                Debug.LogFormat("[Equations X #{0}] With number substitutions: ({1} * sin(({2} * {3}) + {4})) + 21", moduleId, newnumbers[0], newnumbers[1], newnumbers[2], newnumbers[3]);
            }
            else if ((rule1 == true) && (rule3 == false))
            {
                tempanswer = (newnumbers[0] * Mathf.Cos(((newnumbers[1] * newnumbers[2]) + newnumbers[3]) * ((Mathf.PI) / 180))) + 21;
                Debug.LogFormat("[Equations X #{0}] New Equation: (num1 * cos((num2 * num3) + num4)) + 21", moduleId);
                Debug.LogFormat("[Equations X #{0}] With number substitutions: ({1} * cos(({2} * {3}) + {4})) + 21", moduleId, newnumbers[0], newnumbers[1], newnumbers[2], newnumbers[3]);
            }
            else if ((rule1 == false) && (rule3 == true))
            {
                tempanswer = newnumbers[0] * Mathf.Sin(((newnumbers[1] * newnumbers[2]) + newnumbers[3]) * ((Mathf.PI) / 180));
                Debug.LogFormat("[Equations X #{0}] New Equation: num1 * sin((num2 * num3) + num4)", moduleId);
                Debug.LogFormat("[Equations X #{0}] With number substitutions: {1} * sin(({2} * {3}) + {4})", moduleId, newnumbers[0], newnumbers[1], newnumbers[2], newnumbers[3]);
            }
        }
        else if(symbol == 7)
        {
            newnumbers = new float[3];
            float.TryParse(numbers.Substring(0, numbers.IndexOf('.')), out newnumbers[0]);
            float.TryParse(numbers.Substring(numbers.IndexOf('.') + 1, ((numbers.LastIndexOf('.') - 1) - numbers.IndexOf('.'))), out newnumbers[1]);
            float.TryParse(numbers.Substring(numbers.LastIndexOf('.') + 1), out newnumbers[2]);

            Debug.LogFormat("[Equations X #{0}] The symbol is Angular Acceleration", moduleId);
            Debug.LogFormat("[Equations X #{0}] The numbers are {1}, {2}, and {3}", moduleId, newnumbers[0], newnumbers[1], newnumbers[2]);
            Debug.LogFormat("[Equations X #{0}] The initial equation is: (num2 - num3) / num1", moduleId);

            //Start Ang. Acc. Rules
            Debug.LogFormat("[Equations X #{0}] Rule checks for Angular Acceleration...", moduleId);
            bool rule2 = false;
            bool rule3 = false;
            Debug.LogFormat("[Equations X #{0}] Rule 1: False (will always be unless there are 2 strikes!)", moduleId);
            if (bomb.GetPortCount(Port.StereoRCA) >= 1)
            {
                tempanswer = ((newnumbers[1] - newnumbers[2]) / newnumbers[0]) + 8;
                rule2 = true;
            }
            Debug.LogFormat("[Equations X #{0}] Rule 2: {1}", moduleId, rule2);
            if ((bomb.GetModuleNames().Count - bomb.GetSolvableModuleNames().Count) > 0)
            {
                tempanswer = (newnumbers[1] - newnumbers[2]) / (4 * newnumbers[0]);
                rule3 = true;
            }
            Debug.LogFormat("[Equations X #{0}] Rule 3: {1}", moduleId, rule3);
            if (newnumbers[0] == 0)
            {
                typeNothing = true;
                Debug.LogFormat("[Equations X #{0}] Divide by zero occurs, press submit with nothing in input.", moduleId);
            }
            else
            {
                if ((rule2 == true) && (rule3 == true))
                {
                    typeSomething = true;
                    tempanswer = 116;
                }
                Debug.LogFormat("[Equations X #{0}] Rule 4: {1}", moduleId, typeSomething);
                if ((rule2 == false) && (rule3 == false))
                {
                    tempanswer = (newnumbers[1] - newnumbers[2]) / newnumbers[0];
                    Debug.LogFormat("[Equations X #{0}] New Equation: (num2 - num3) / num1", moduleId);
                    Debug.LogFormat("[Equations X #{0}] With number substitutions: (({2} - {3}) / {1})", moduleId, newnumbers[0], newnumbers[1], newnumbers[2]);
                }
                //purely for logging
                else if ((rule2 == true) && (rule3 == true))
                {
                    Debug.LogFormat("[Equations X #{0}] No equation necessary with Rule 4", moduleId);
                }
                else if ((rule2 == true) && (rule3 == false))
                {
                    Debug.LogFormat("[Equations X #{0}] New Equation: ((num2 - num3) / num1) + 8", moduleId);
                    Debug.LogFormat("[Equations X #{0}] With number substitutions: (({2} - {3}) / {1}) + 8", moduleId, newnumbers[0], newnumbers[1], newnumbers[2]);
                }
                else if ((rule2 == false) && (rule3 == true))
                {
                    Debug.LogFormat("[Equations X #{0}] New Equation: ((num2 - num3) / (4 * num1))", moduleId);
                    Debug.LogFormat("[Equations X #{0}] With number substitutions: (({2} - {3}) / (4 * {1}))", moduleId, newnumbers[0], newnumbers[1], newnumbers[2]);
                }
            }
        }
        else
        {
            newnumbers = new float[2];
            float.TryParse(numbers.Substring(0, numbers.IndexOf('.')), out newnumbers[0]);
            float.TryParse(numbers.Substring(numbers.LastIndexOf('.') + 1), out newnumbers[1]);

            if (symbol == 0)
            {
                Debug.LogFormat("[Equations X #{0}] The symbol is H(T)", moduleId);
                Debug.LogFormat("[Equations X #{0}] The numbers are {1} and {2}", moduleId, newnumbers[0], newnumbers[1]);
                Debug.LogFormat("[Equations X #{0}] The initial equation is: integral(num1^2 + (4 * num2))dT", moduleId);
                Debug.LogFormat("[Equations X #{0}] The initial C-value is: -5", moduleId);
                //Start H of T Rules
                bool rule1 = false;
                bool rule2 = false;
                int cval = -5;
                Debug.LogFormat("[Equations X #{0}] Rule checks for H(T)...", moduleId);
                if (bomb.GetBatteryCount() > 5)
                {
                    tempanswer = (newnumbers[0] * newnumbers[0]) + (4 * newnumbers[1]);
                    rule1 = true;
                }
                Debug.LogFormat("[Equations X #{0}] Rule 1: {1}", moduleId, rule1);
                if (bomb.GetPortCount(Port.Parallel) >= 1)
                {
                    cval = 8;
                    rule2 = true;
                }
                Debug.LogFormat("[Equations X #{0}] Rule 2: {1}", moduleId, rule2);
                if (rule1 == false)
                {
                    tempanswer = (((newnumbers[0] * newnumbers[0] * newnumbers[0]) / 3) + (2 * (newnumbers[1] * newnumbers[1])) + cval);
                    Debug.LogFormat("[Equations X #{0}] New C-value: {1}", moduleId, cval);
                    Debug.LogFormat("[Equations X #{0}] New Equation: ((1/3) * num1^3) + (2 * num2^2) + C", moduleId);
                    Debug.LogFormat("[Equations X #{0}] With number substitutions: ((1/3) * {1}^3) + (2 * {2}^2) + {3}", moduleId, newnumbers[0], newnumbers[1], cval);
                }
                //purely for logging
                else if (rule1 == true)
                {
                    Debug.LogFormat("[Equations X #{0}] New C-value: {1}", moduleId, cval);
                    Debug.LogFormat("[Equations X #{0}] New Equation: num1^2 + (4 * num2)", moduleId);
                    Debug.LogFormat("[Equations X #{0}] With number substitutions: {1}^2 + (4 * {2})", moduleId, newnumbers[0], newnumbers[1]);
                }
            }
            else if (symbol == 1)
            {
                Debug.LogFormat("[Equations X #{0}] The symbol is Power", moduleId);
                Debug.LogFormat("[Equations X #{0}] The numbers are {1} and {2} (Rule 3 makes these subject to change)", moduleId, newnumbers[0], newnumbers[1]);
                Debug.LogFormat("[Equations X #{0}] The initial equation is: num1 * num2", moduleId);
                //Start Power Rules
                Debug.LogFormat("[Equations X #{0}] Rule checks for Power...", moduleId);
                bool rule1 = false;
                bool rule2 = false;
                bool rule3 = false;
                if (serialHasVowels())
                {
                    rule1 = true;
                }
                Debug.LogFormat("[Equations X #{0}] Rule 1: {1}", moduleId, rule1);
                if (bomb.GetModuleNames().Count >= 6)
                {
                    rule2 = true;
                }
                Debug.LogFormat("[Equations X #{0}] Rule 2: {1}", moduleId, rule2);
                if (bomb.IsIndicatorOff("CLR"))
                {
                    if (numbers.Contains('3'))
                    {
                        numbers.Replace('3','4');
                        float.TryParse(numbers.Substring(0, numbers.IndexOf('.')), out newnumbers[0]);
                        float.TryParse(numbers.Substring(numbers.LastIndexOf('.') + 1), out newnumbers[1]);
                    }
                    rule3 = true;
                }
                Debug.LogFormat("[Equations X #{0}] Rule 3: {1}", moduleId, rule3);
                if ((rule1 == false) && (rule2 == false))
                {
                    tempanswer = (newnumbers[0] * newnumbers[1]);
                    Debug.LogFormat("[Equations X #{0}] New Equation: num1 * num2", moduleId);
                    Debug.LogFormat("[Equations X #{0}] With number substitutions: {1} * {2}", moduleId, newnumbers[0], newnumbers[1]);
                }
                else if ((rule1 == false) && (rule2 == true))
                {
                    tempanswer = (newnumbers[0] * newnumbers[1]) + 14;
                    Debug.LogFormat("[Equations X #{0}] New Equation: (num1 * num2) + 14", moduleId);
                    Debug.LogFormat("[Equations X #{0}] With number substitutions: ({1} * {2}) + 14", moduleId, newnumbers[0], newnumbers[1]);
                }
                else if ((rule1 == true) && (rule2 == false))
                {
                    tempanswer = ((2) * (newnumbers[0] * newnumbers[1])) / 3;
                    Debug.LogFormat("[Equations X #{0}] New Equation: (2/3) * (num1 * num2)", moduleId);
                    Debug.LogFormat("[Equations X #{0}] With number substitutions: (2/3) * ({1} * {2})", moduleId, newnumbers[0], newnumbers[1]);
                }
                else if ((rule1 == true) && (rule2 == true))
                {
                    tempanswer = (((2) * (newnumbers[0] * newnumbers[1])) / 3) + 14;
                    Debug.LogFormat("[Equations X #{0}] New Equation: ((2/3) * (num1 * num2)) + 14", moduleId);
                    Debug.LogFormat("[Equations X #{0}] With number substitutions: ((2/3) * ({1} * {2})) + 14", moduleId, newnumbers[0], newnumbers[1]);
                }
            }
            else if (symbol == 3)
            {
                Debug.LogFormat("[Equations X #{0}] The symbol is Angular Velocity", moduleId);
                Debug.LogFormat("[Equations X #{0}] The numbers are {1} and {2}", moduleId, newnumbers[0], newnumbers[1]);
                Debug.LogFormat("[Equations X #{0}] The initial equation is: num2 / num1", moduleId);
                //Start angular velocity Rules
                bool rule1 = false;
                bool rule2 = false;
                Debug.LogFormat("[Equations X #{0}] Rule checks for Angular Velocity...", moduleId);
                if (serialHasOdd())
                {
                    tempanswer = (newnumbers[1] / newnumbers[0]) - 5;
                    rule1 = true;
                }
                Debug.LogFormat("[Equations X #{0}] Rule 1: {1}", moduleId, rule1);
                if ((bomb.IsIndicatorOff("CAR")) && (bomb.IsIndicatorOff("IND")))
                {
                    tempanswer = (newnumbers[0] / newnumbers[1]);
                    rule2 = true;
                }
                Debug.LogFormat("[Equations X #{0}] Rule 2: {1}", moduleId, rule2);
                if(newnumbers[0] == 0)
                {
                    typeNothing = true;
                    Debug.LogFormat("[Equations X #{0}] Divide by zero occurs, press submit with nothing in input.", moduleId);
                }
                else if ((rule1 == true) && (rule2 == true))
                {
                    tempanswer = (newnumbers[0] / newnumbers[1]) - 5;
                    Debug.LogFormat("[Equations X #{0}] New Equation: (num1 / num2) - 5", moduleId);
                    Debug.LogFormat("[Equations X #{0}] With number substitutions: ({1} / {2}) - 5", moduleId, newnumbers[0], newnumbers[1]);
                }
                else if((rule1 == false) && (rule2 == false))
                {
                    tempanswer = (newnumbers[1] / newnumbers[0]);
                    Debug.LogFormat("[Equations X #{0}] New Equation: num2 / num1", moduleId);
                    Debug.LogFormat("[Equations X #{0}] With number substitutions: {2} / {1}", moduleId, newnumbers[0], newnumbers[1]);
                }
                //purely for logging
                else if ((rule1 == true) && (rule2 == false))
                {
                    Debug.LogFormat("[Equations X #{0}] New Equation: (num2 / num1) - 5", moduleId);
                    Debug.LogFormat("[Equations X #{0}] With number substitutions: ({2} / {1}) - 5", moduleId, newnumbers[0], newnumbers[1]);
                }
                else  if ((rule1 == false) && (rule2 == true))
                {
                    Debug.LogFormat("[Equations X #{0}] New Equation: num1 / num2", moduleId);
                    Debug.LogFormat("[Equations X #{0}] With number substitutions: {1} / {2}", moduleId, newnumbers[0], newnumbers[1]);
                }
            }
            else if (symbol == 4)
            {
                Debug.LogFormat("[Equations X #{0}] The symbol is Z(T)", moduleId);
                Debug.LogFormat("[Equations X #{0}] The numbers are {1} and {2}", moduleId, newnumbers[0], newnumbers[1]);
                Debug.LogFormat("[Equations X #{0}] The initial equation is: integral(num1 + 3)dT", moduleId);
                Debug.LogFormat("[Equations X #{0}] The initial C-value is: 2", moduleId);
                //Start Z of T Rules
                bool rule1 = false;
                Debug.LogFormat("[Equations X #{0}] Rule checks for Z(T)...", moduleId);
                if (getWidgetCount() > 6)
                {
                    tempanswer = newnumbers[0] + 3;
                    rule1 = true;
                }
                Debug.LogFormat("[Equations X #{0}] Rule 1: {1}", moduleId, rule1);
                if (rule1 == false)
                {
                    tempanswer = (((newnumbers[0] * newnumbers[0]) / 2) + (3 * newnumbers[1]) + 2);
                    Debug.LogFormat("[Equations X #{0}] New C-value: 2", moduleId);
                    Debug.LogFormat("[Equations X #{0}] New Equation: ((1/2) * num1^2) + (3 * num2) + C", moduleId);
                    Debug.LogFormat("[Equations X #{0}] With number substitutions: ((1/2) * {1}^2) + (3 * {2}) + 2", moduleId, newnumbers[0], newnumbers[1]);
                }
                //purely for logging
                else if(rule1 == true)
                {
                    Debug.LogFormat("[Equations X #{0}] New C-value: 2", moduleId);
                    Debug.LogFormat("[Equations X #{0}] New Equation: num1 + 3", moduleId);
                    Debug.LogFormat("[Equations X #{0}] With number substitutions: {1} + 3", moduleId, newnumbers[0]);
                }
            }
            else if (symbol == 5)
            {
                Debug.LogFormat("[Equations X #{0}] The symbol is Torque", moduleId);
                Debug.LogFormat("[Equations X #{0}] The numbers are {1} and {2}", moduleId, newnumbers[0], newnumbers[1]);
                Debug.LogFormat("[Equations X #{0}] The initial equation is: num1 * num2", moduleId);
                //Start torque Rules
                bool rule1 = false;
                bool rule2 = false;
                bool rule3 = false;
                bool rule4 = false;
                bool rule5 = false;
                Debug.LogFormat("[Equations X #{0}] Rule checks for Torque...", moduleId);

                if ((bomb.GetBatteryCount() > 1) && isAnyPlateEmpty())
                {
                    rule1 = true;
                }
                Debug.LogFormat("[Equations X #{0}] Rule 1: {1}", moduleId, rule1);
                if (bomb.GetSolvedModuleNames().Count >= 2)
                {
                    rule2 = true;
                }
                Debug.LogFormat("[Equations X #{0}] Rule 2: {1}", moduleId, rule2);
                if (bomb.IsIndicatorOn("FRQ"))
                {
                    rule3 = true;
                }
                Debug.LogFormat("[Equations X #{0}] Rule 3: {1}", moduleId, rule3);
                if (!bomb.IsIndicatorOn("FRQ"))
                {
                    if ((bomb.GetModuleNames().Count - bomb.GetSolvableModuleNames().Count) > 0)
                    {
                        rule4 = true;
                    }
                    Debug.LogFormat("[Equations X #{0}] Rule 4: {1}", moduleId, rule4);
                    if (bomb.IsIndicatorOff("BOB"))
                    {
                        rule5 = true;
                    }
                    Debug.LogFormat("[Equations X #{0}] Rule 5: {1}", moduleId, rule5);
                }

                if (rule1 == true)
                {
                    tempanswer = (newnumbers[0] * newnumbers[1]) + 10;
                    Debug.LogFormat("[Equations X #{0}] New Equation: (num1 * num2) + 10", moduleId);
                    Debug.LogFormat("[Equations X #{0}] With number substitutions: ({1} * {2}) + 10", moduleId, newnumbers[0], newnumbers[1]);
                }
                if (rule2 == true && rule1 == false)
                {
                    tempanswer = ((newnumbers[0] / 2) * (newnumbers[1] / 2));
                    Debug.LogFormat("[Equations X #{0}] New Equation: ((num1 / 2) * (num2 / 2))", moduleId);
                    Debug.LogFormat("[Equations X #{0}] With number substitutions: (({1} / 2) * ({2} / 2))", moduleId, newnumbers[0], newnumbers[1]);
                }
                else if (rule2 == true && rule1 == true)
                {
                    tempanswer = ((newnumbers[0] / 2) * (newnumbers[1] / 2)) + 5;
                    Debug.LogFormat("[Equations X #{0}] New Equation: ((num1 / 2) * (num2 / 2)) + 5", moduleId);
                    Debug.LogFormat("[Equations X #{0}] With number substitutions: (({1} / 2) * ({2} / 2)) + 5", moduleId, newnumbers[0], newnumbers[1]);
                }
                if (rule4 == true)
                {
                    tempanswer = (newnumbers[0] * newnumbers[1]);
                    Debug.LogFormat("[Equations X #{0}] New Equation: num1 * num2", moduleId);
                    Debug.LogFormat("[Equations X #{0}] With number substitutions: {1} * {2}", moduleId, newnumbers[0], newnumbers[1]);
                }
                if (rule5 == true && rule4 == true)
                {
                    tempanswer = (newnumbers[0] * newnumbers[1]) + 3;
                    Debug.LogFormat("[Equations X #{0}] New Equation: (num1 * num2) + 3", moduleId);
                    Debug.LogFormat("[Equations X #{0}] With number substitutions: ({1} * {2}) + 3", moduleId, newnumbers[0], newnumbers[1]);
                }
                else if (rule5 == true && rule1 == true && rule2 == false)
                {
                    tempanswer = (newnumbers[0] * newnumbers[1]) + 13;
                    Debug.LogFormat("[Equations X #{0}] New Equation: (num1 * num2) + 13", moduleId);
                    Debug.LogFormat("[Equations X #{0}] With number substitutions: ({1} * {2}) + 13", moduleId, newnumbers[0], newnumbers[1]);
                }
                else if (rule5 == true && rule1 == false && rule2 == false)
                {
                    tempanswer = (newnumbers[0] * newnumbers[1]) + 3;
                    Debug.LogFormat("[Equations X #{0}] New Equation: (num1 * num2) + 3", moduleId);
                    Debug.LogFormat("[Equations X #{0}] With number substitutions: ({1} * {2}) + 3", moduleId, newnumbers[0], newnumbers[1]);
                }
                else if (rule5 == true && rule1 == true && rule2 == true)
                {
                    tempanswer = ((newnumbers[0] / 2) * (newnumbers[1] / 2)) + 8;
                    Debug.LogFormat("[Equations X #{0}] New Equation: ((num1 / 2) * (num2 / 2)) + 8", moduleId);
                    Debug.LogFormat("[Equations X #{0}] With number substitutions: (({1} / 2) * ({2} / 2)) + 8", moduleId, newnumbers[0], newnumbers[1]);
                }
                else if (rule5 == true && rule1 == false && rule2 == true)
                {
                    tempanswer = ((newnumbers[0] / 2) * (newnumbers[1] / 2)) + 3;
                    Debug.LogFormat("[Equations X #{0}] New Equation: ((num1 / 2) * (num2 / 2)) + 3", moduleId);
                    Debug.LogFormat("[Equations X #{0}] With number substitutions: (({1} / 2) * ({2} / 2)) + 3", moduleId, newnumbers[0], newnumbers[1]);
                }
                if (rule1 == false && rule2 == false && rule5 == false)
                {
                    tempanswer = (newnumbers[0] * newnumbers[1]);
                    Debug.LogFormat("[Equations X #{0}] New Equation: num1 * num2", moduleId);
                    Debug.LogFormat("[Equations X #{0}] With number substitutions: {1} * {2}", moduleId, newnumbers[0], newnumbers[1]);
                }
            }
            else if (symbol == 6)
            {
                Debug.LogFormat("[Equations X #{0}] The symbol is Coefficient of Static Friction", moduleId);
                Debug.LogFormat("[Equations X #{0}] The numbers are {1} and {2}", moduleId, newnumbers[0], newnumbers[1]);
                Debug.LogFormat("[Equations X #{0}] The initial equation is: num2 / num1", moduleId);
                //Start coe. of Stat. Fric. Rules
                Debug.LogFormat("[Equations X #{0}] Rule checks for Coefficient of Static Friction...", moduleId);
                bool rule1 = false;
                bool rule2 = false;
                bool rule3 = false;
                if (bomb.GetBatteryCount() == 2)
                {
                    tempanswer = ((7 * newnumbers[1]) / (3 * newnumbers[0]));
                    rule1 = true;
                }
                Debug.LogFormat("[Equations X #{0}] Rule 1: {1}", moduleId, rule1);
                if (!bomb.IsIndicatorOn("NSA") && bomb.IsPortPresent("RJ45") && (rule1 == false))
                {
                    tempanswer = (newnumbers[1] / newnumbers[0]) + 1;
                    rule3 = true;
                }
                else if (!bomb.IsIndicatorOn("NSA") && bomb.IsPortPresent("RJ45") && (rule1 == true))
                {
                    tempanswer = ((7 * newnumbers[1]) / (3 * newnumbers[0])) + 1;
                    rule3 = true;
                }
                //for logging only
                if (bomb.IsIndicatorOn("NSA"))
                {
                    rule2 = true;
                }
                //normal code continued
                Debug.LogFormat("[Equations X #{0}] Rule 2: {1}", moduleId, rule2);
                Debug.LogFormat("[Equations X #{0}] Rule 3: {1}", moduleId, rule3);
                if (newnumbers[0] == 0)
                {
                    typeNothing = true;
                    Debug.LogFormat("[Equations X #{0}] Divide by zero occurs, press submit with nothing in input.", moduleId);
                }
                else if ((rule1 == false) && (rule3 == false))
                {
                    tempanswer = (newnumbers[1] / newnumbers[0]);
                    Debug.LogFormat("[Equations X #{0}] New Equation: num2 / num1", moduleId);
                    Debug.LogFormat("[Equations X #{0}] With number substitutions: {2} / {1}", moduleId, newnumbers[0], newnumbers[1]);
                }
                //purely for logging
                else if ((rule1 == false) && (rule3 == true))
                {
                    Debug.LogFormat("[Equations X #{0}] New Equation: (num2 / num1) + 1", moduleId);
                    Debug.LogFormat("[Equations X #{0}] With number substitutions: ({2} / {1}) + 1", moduleId, newnumbers[0], newnumbers[1]);
                }
                else if ((rule1 == true) && (rule3 == false))
                {
                    Debug.LogFormat("[Equations X #{0}] New Equation: ((7 * num2) / (3 * num1))", moduleId);
                    Debug.LogFormat("[Equations X #{0}] With number substitutions: ((7 * {2}) / (3 * {1}))", moduleId, newnumbers[0], newnumbers[1]);
                }
                else if ((rule1 == true) && (rule3 == true))
                {
                    Debug.LogFormat("[Equations X #{0}] New Equation: ((7 * num2) / (3 * num1)) + 1", moduleId);
                    Debug.LogFormat("[Equations X #{0}] With number substitutions: ((7 * {2}) / (3 * {1})) + 1", moduleId, newnumbers[0], newnumbers[1]);
                }
            }
            else if (symbol == 8)
            {
                Debug.LogFormat("[Equations X #{0}] The symbol is Kinetic Energy", moduleId);
                Debug.LogFormat("[Equations X #{0}] The numbers are {1} and {2}", moduleId, newnumbers[0], newnumbers[1]);
                Debug.LogFormat("[Equations X #{0}] The initial equation is: (1/2) * num1 * (num2)^2", moduleId);
                //Start K. energy Rules
                Debug.LogFormat("[Equations X #{0}] Rule checks for Kinetic Energy...", moduleId);
                bool rule1 = false;
                bool rule2 = false;
                if (getIndicatorCount() >= 3)
                {
                    tempanswer = (newnumbers[0] * (newnumbers[1] * newnumbers[1]));
                    rule1 = true;
                }
                Debug.LogFormat("[Equations X #{0}] Rule 1: {1}", moduleId, rule1);
                if ((rule1 == false) && bombHasModule("The Button"))
                {
                    tempanswer = ((1.5F) * (newnumbers[0] * (newnumbers[1] * newnumbers[1])));
                    rule2 = true;
                }
                if ((rule1 == true) && bombHasModule("The Button"))
                {
                    tempanswer = (3 * (newnumbers[0] * (newnumbers[1] * newnumbers[1])));
                    rule2 = true;
                }
                Debug.LogFormat("[Equations X #{0}] Rule 2: {1}", moduleId, rule2);
                if ((rule1 == false) && (rule2 == false))
                {
                    tempanswer = ((0.5F) * (newnumbers[0] * (newnumbers[1] * newnumbers[1])));
                    Debug.LogFormat("[Equations X #{0}] New Equation: (1/2) * num1 * (num2)^2", moduleId);
                    Debug.LogFormat("[Equations X #{0}] With number substitutions: (1/2) * {1} * {2}^2", moduleId, newnumbers[0], newnumbers[1]);
                }
                //Purely for logging
                else if ((rule1 == true) && (rule2 == true))
                {
                    Debug.LogFormat("[Equations X #{0}] New Equation: 3 * num1 * (num2)^2", moduleId);
                    Debug.LogFormat("[Equations X #{0}] With number substitutions: 3 * {1} * {2}^2", moduleId, newnumbers[0], newnumbers[1]);
                }
                else if ((rule1 == true) && (rule2 == false))
                {
                    Debug.LogFormat("[Equations X #{0}] New Equation: num1 * (num2)^2", moduleId);
                    Debug.LogFormat("[Equations X #{0}] With number substitutions: {1} * {2}^2", moduleId, newnumbers[0], newnumbers[1]);
                }
                else if ((rule1 == false) && (rule2 == true))
                {
                    Debug.LogFormat("[Equations X #{0}] New Equation: (3/2) * num1 * (num2)^2", moduleId);
                    Debug.LogFormat("[Equations X #{0}] With number substitutions: (3/2) * {1} * {2}^2", moduleId, newnumbers[0], newnumbers[1]);
                }
            }
        }
        return tempanswer;
    }

    private int getIndicatorCount()
    {
        int tempcount = 0;
        if (bomb.IsIndicatorOn("NSA"))
        {
            tempcount++;
        }
        if (bomb.IsIndicatorOff("NSA"))
        {
            tempcount++;
        }
        if (bomb.IsIndicatorOn("MSA"))
        {
            tempcount++;
        }
        if (bomb.IsIndicatorOff("MSA"))
        {
            tempcount++;
        }
        if (bomb.IsIndicatorOn("CAR"))
        {
            tempcount++;
        }
        if (bomb.IsIndicatorOff("CAR"))
        {
            tempcount++;
        }
        if (bomb.IsIndicatorOn("SND"))
        {
            tempcount++;
        }
        if (bomb.IsIndicatorOff("SND"))
        {
            tempcount++;
        }
        if (bomb.IsIndicatorOn("IND"))
        {
            tempcount++;
        }
        if (bomb.IsIndicatorOff("IND"))
        {
            tempcount++;
        }
        if (bomb.IsIndicatorOn("CLR"))
        {
            tempcount++;
        }
        if (bomb.IsIndicatorOff("CLR"))
        {
            tempcount++;
        }
        if (bomb.IsIndicatorOn("SIG"))
        {
            tempcount++;
        }
        if (bomb.IsIndicatorOff("SIG"))
        {
            tempcount++;
        }
        if (bomb.IsIndicatorOn("TRN"))
        {
            tempcount++;
        }
        if (bomb.IsIndicatorOff("TRN"))
        {
            tempcount++;
        }
        if (bomb.IsIndicatorOn("FRQ"))
        {
            tempcount++;
        }
        if (bomb.IsIndicatorOff("FRQ"))
        {
            tempcount++;
        }
        if (bomb.IsIndicatorOn("FRK"))
        {
            tempcount++;
        }
        if (bomb.IsIndicatorOff("FRK"))
        {
            tempcount++;
        }
        if (bomb.IsIndicatorOn("BOB"))
        {
            tempcount++;
        }
        if (bomb.IsIndicatorOff("BOB"))
        {
            tempcount++;
        }
        return tempcount;
    }

    private float reCalcPosition(bool rule1, float num1, float num2, float num3, float num4)
    {
        float tempanswer;
        Debug.LogFormat("[Equations X #{0}] Rule 3 is now true! Recalculating answer...", moduleId);
        if (rule1 == true)
        {
            tempanswer = (num1 * Mathf.Sin(((num2 * num3) + num4) * ((Mathf.PI) / 180))) + 21;
            Debug.LogFormat("[Equations X #{0}] New Equation: (num1 * sin((num2 * num3) + num4)) + 21", moduleId);
            Debug.LogFormat("[Equations X #{0}] With number substitutions: ({1} * sin(({2} * {3}) + {4})) + 21", moduleId, num1, num2, num3, num4);
        }
        else
        {
            tempanswer = num1 * Mathf.Sin(((num2 * num3) + num4) * ((Mathf.PI) / 180));
            Debug.LogFormat("[Equations X #{0}] New Equation: num1 * sin((num2 * num3) + num4)", moduleId);
            Debug.LogFormat("[Equations X #{0}] With number substitutions: {1} * sin(({2} * {3}) + {4})", moduleId, num1, num2, num3, num4);
        }
        return tempanswer;
    }

    private float reCalcTorque(bool rule1, bool rule2, bool rule3, bool rule4, bool rule5, float num1, float num2)
    {
        float tempanswer = 0;
        Debug.LogFormat("[Equations X #{0}] Rule 2 is now true! Recalculating answer...", moduleId);
        if (rule3 == true)
        {
            rule4 = false;
            rule5 = false;
        }
        if (rule1 == true)
        {
            tempanswer = (newnumbers[0] * newnumbers[1]) + 10;
            Debug.LogFormat("[Equations X #{0}] New Equation: (num1 * num2) + 10", moduleId);
            Debug.LogFormat("[Equations X #{0}] With number substitutions: ({1} * {2}) + 10", moduleId, newnumbers[0], newnumbers[1]);
        }
        if (rule2 == true && rule1 == false)
        {
            tempanswer = ((newnumbers[0] / 2) * (newnumbers[1] / 2));
            Debug.LogFormat("[Equations X #{0}] New Equation: ((num1 / 2) * (num2 / 2))", moduleId);
            Debug.LogFormat("[Equations X #{0}] With number substitutions: (({1} / 2) * ({2} / 2))", moduleId, newnumbers[0], newnumbers[1]);
        }
        else if (rule2 == true && rule1 == true)
        {
            tempanswer = ((newnumbers[0] / 2.0F) * (float)(newnumbers[1] / 2.0F)) + 5.0F;
            Debug.LogFormat("[Equations X #{0}] New Equation: ((num1 / 2) * (num2 / 2)) + 5", moduleId);
            Debug.LogFormat("[Equations X #{0}] With number substitutions: (({1} / 2) * ({2} / 2)) + 5", moduleId, newnumbers[0], newnumbers[1]);
        }
        if (rule4 == true)
        {
            tempanswer = (newnumbers[0] * newnumbers[1]);
            Debug.LogFormat("[Equations X #{0}] New Equation: num1 * num2", moduleId);
            Debug.LogFormat("[Equations X #{0}] With number substitutions: {1} * {2}", moduleId, newnumbers[0], newnumbers[1]);
        }
        if (rule5 == true && rule4 == true)
        {
            tempanswer = (newnumbers[0] * newnumbers[1]) + 3;
            Debug.LogFormat("[Equations X #{0}] New Equation: (num1 * num2) + 3", moduleId);
            Debug.LogFormat("[Equations X #{0}] With number substitutions: ({1} * {2}) + 3", moduleId, newnumbers[0], newnumbers[1]);
        }
        else if (rule5 == true && rule1 == true && rule2 == false)
        {
            tempanswer = (newnumbers[0] * newnumbers[1]) + 13;
            Debug.LogFormat("[Equations X #{0}] New Equation: (num1 * num2) + 13", moduleId);
            Debug.LogFormat("[Equations X #{0}] With number substitutions: ({1} * {2}) + 13", moduleId, newnumbers[0], newnumbers[1]);
        }
        else if (rule5 == true && rule1 == false && rule2 == false)
        {
            tempanswer = (newnumbers[0] * newnumbers[1]) + 3;
            Debug.LogFormat("[Equations X #{0}] New Equation: (num1 * num2) + 3", moduleId);
            Debug.LogFormat("[Equations X #{0}] With number substitutions: ({1} * {2}) + 3", moduleId, newnumbers[0], newnumbers[1]);
        }
        else if (rule5 == true && rule1 == true && rule2 == true)
        {
            tempanswer = ((newnumbers[0] / 2) * (newnumbers[1] / 2)) + 8;
            Debug.LogFormat("[Equations X #{0}] New Equation: ((num1 / 2) * (num2 / 2)) + 8", moduleId);
            Debug.LogFormat("[Equations X #{0}] With number substitutions: (({1} / 2) * ({2} / 2)) + 8", moduleId, newnumbers[0], newnumbers[1]);
        }
        else if (rule5 == true && rule1 == false && rule2 == true)
        {
            tempanswer = ((newnumbers[0] / 2) * (newnumbers[1] / 2)) + 3;
            Debug.LogFormat("[Equations X #{0}] New Equation: ((num1 / 2) * (num2 / 2)) + 3", moduleId);
            Debug.LogFormat("[Equations X #{0}] With number substitutions: (({1} / 2) * ({2} / 2)) + 3", moduleId, newnumbers[0], newnumbers[1]);
        }
        if (rule1 == false && rule2 == false && rule5 == false)
        {
            tempanswer = (newnumbers[0] * newnumbers[1]);
            Debug.LogFormat("[Equations X #{0}] New Equation: num1 * num2", moduleId);
            Debug.LogFormat("[Equations X #{0}] With number substitutions: {1} * {2}", moduleId, newnumbers[0], newnumbers[1]);
        }
        return tempanswer;
    }

    private int getWidgetCount()
    {
        int tempcount = 0;
        tempcount += bomb.GetBatteryHolderCount();
        tempcount += bomb.GetPortPlateCount();
        tempcount += getIndicatorCount();
        return tempcount;
    }

    private bool bombHasModule(string name)
    {
        List<string> modules = bomb.GetModuleNames();
        foreach(string mod in modules)
        {
            if (mod.EqualsIgnoreCase(name))
            {
                return true;
            }
        }
        return false;
    }

    private bool serialHasOdd()
    {
        string serial = bomb.GetSerialNumber();
        if (serial.Contains("1") || serial.Contains("3") || serial.Contains("5") || serial.Contains("7") || serial.Contains("9"))
        {
            return true;
        }
        return false;
    }

    private bool serialHasVowels()
    {
        string serial = bomb.GetSerialNumber();
        if (serial.Contains("A") || serial.Contains("E") || serial.Contains("I") || serial.Contains("O") || serial.Contains("U"))
        {
            return true;
        }
        return false;
    }

    private bool isAnyPlateEmpty()
    {
        foreach(string[] plateports in bomb.GetPortPlates())
        {
            if(plateports.Length == 0)
            {
                return true;
            }
        }
        return false;
    }

    private string randomCheer()
    {
        int cheernum = Random.RandomRange(0, 5);
        return cheers[cheernum];
    }

    //twitch plays
    #pragma warning disable 414
    //private readonly string TwitchHelpMessage = @"!{0} enter 1024 [Enters the number '1024' into the input display] | !{0} clear [Clears the input display] | !{0} submit [Submits what is in the input display] | !{0} submit 1024 [Enters the number '1024' into the input display AND submits it]";
    private readonly string TwitchHelpMessage = @"!{0} submit <num> [Submits the specified number] | !{0} nothing [Presses submit with no inputs]";
    #pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string command)
    {
        /**if (Regex.IsMatch(command, @"^\s*clear\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            yield return new[] { buttons[10] };
            yield break;
        }
        string[] parameters = command.Split(' ');
        if (Regex.IsMatch(command, @"^\s*submit\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant) && (parameters.Length <= 1))
        {
            yield return null;
            yield return new[] { buttons[11] };
            yield break;
        }*/
        //This was not here originally
        if (Regex.IsMatch(command, @"^\s*nothing\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            buttons[10].OnInteract();
            yield return new WaitForSeconds(.2f);
            buttons[11].OnInteract();
            yield break;
        }
        string[] parameters = command.Split(' ');
        //end originally not here
        if (Regex.IsMatch(parameters[0], @"^\s*submit\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant) && !(parameters.Length <= 1))
        {
            var buttonsToPress = new List<KMSelectable>();
            buttonsToPress.Add(buttons[10]);
            char[] integers = parameters[1].ToCharArray();
            foreach (char num in integers)
            {
                if (num.Equals('0'))
                {
                    buttonsToPress.Add(buttons[0]);
                }
                else if (num.Equals('1'))
                {
                    buttonsToPress.Add(buttons[1]);
                }
                else if (num.Equals('2'))
                {
                    buttonsToPress.Add(buttons[2]);
                }
                else if (num.Equals('3'))
                {
                    buttonsToPress.Add(buttons[3]);
                }
                else if (num.Equals('4'))
                {
                    buttonsToPress.Add(buttons[4]);
                }
                else if (num.Equals('5'))
                {
                    buttonsToPress.Add(buttons[5]);
                }
                else if (num.Equals('6'))
                {
                    buttonsToPress.Add(buttons[6]);
                }
                else if (num.Equals('7'))
                {
                    buttonsToPress.Add(buttons[7]);
                }
                else if (num.Equals('8'))
                {
                    buttonsToPress.Add(buttons[8]);
                }
                else if (num.Equals('9'))
                {
                    buttonsToPress.Add(buttons[9]);
                }
                else
                {
                    yield break;
                }
            }
            buttonsToPress.Add(buttons[11]);
            yield return null;
            foreach (KMSelectable km in buttonsToPress)
            {
                km.OnInteract();
                yield return new WaitForSeconds(.2f);
            }
            yield break;
        }
        /**if (Regex.IsMatch(parameters[0], @"^\s*enter\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant) && !(parameters.Length <= 1))
        {
            var buttonsToPress = new List<KMSelectable>();
            buttonsToPress.Add(buttons[10]);
            char[] integers = parameters[1].ToCharArray();
            foreach (char num in integers)
            {
                if (num.Equals('0'))
                {
                    buttonsToPress.Add(buttons[0]);
                }
                else if (num.Equals('1'))
                {
                    buttonsToPress.Add(buttons[1]);
                }
                else if (num.Equals('2'))
                {
                    buttonsToPress.Add(buttons[2]);
                }
                else if (num.Equals('3'))
                {
                    buttonsToPress.Add(buttons[3]);
                }
                else if (num.Equals('4'))
                {
                    buttonsToPress.Add(buttons[4]);
                }
                else if (num.Equals('5'))
                {
                    buttonsToPress.Add(buttons[5]);
                }
                else if (num.Equals('6'))
                {
                    buttonsToPress.Add(buttons[6]);
                }
                else if (num.Equals('7'))
                {
                    buttonsToPress.Add(buttons[7]);
                }
                else if (num.Equals('8'))
                {
                    buttonsToPress.Add(buttons[8]);
                }
                else if (num.Equals('9'))
                {
                    buttonsToPress.Add(buttons[9]);
                }
                else
                {
                    yield break;
                }
            }
            yield return null;
            foreach (KMSelectable km in buttonsToPress)
            {
                km.OnInteract();
                yield return new WaitForSeconds(.2f);
            }
            yield break;
        }*/
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        if(typeNothing != true)
        {
            yield return ProcessTwitchCommand("submit " + answersimp);
        }
        else
        {
            yield return ProcessTwitchCommand("nothing");
        }
    }
}
