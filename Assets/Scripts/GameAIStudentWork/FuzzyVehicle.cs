// compile_check
// Remove the line above if you are submitting to GradeScope for a grade. But leave it if you only want to check
// that your code compiles and the autograder can access your public methods.

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using GameAI;

// All the Fuzz
using Tochas.FuzzyLogic;
using Tochas.FuzzyLogic.MembershipFunctions;
using Tochas.FuzzyLogic.Evaluators;
using Tochas.FuzzyLogic.Mergers;
using Tochas.FuzzyLogic.Defuzzers;
using Tochas.FuzzyLogic.Expressions;

namespace GameAICourse
{

  public class FuzzyVehicle : AIVehicle
  {

    // TODO create some Fuzzy Set enumeration types, and member variables for:
    // Fuzzy Sets (input and output), one or more Fuzzy Value Sets, and Fuzzy
    // Rule Sets for each output.
    // Also, create some methods to instantiate each of the member variables

    // Here are some basic examples to get you started
    enum FzOutputThrottle { Brake, Coast, Accelerate }
    enum FzOutputWheel { TurnLeft, Straight, TurnRight }

    enum FzInputSpeed { Slow, Medium, Fast }
    enum FzInputDirection { Left, Straight, Right }

    FuzzySet<FzInputSpeed> fzSpeedSet;
    FuzzySet<FzInputDirection> fzDirectionSet;

    FuzzySet<FzOutputThrottle> fzThrottleSet;
    FuzzyRuleSet<FzOutputThrottle> fzThrottleRuleSet;

    FuzzySet<FzOutputWheel> fzWheelSet;
    FuzzyRuleSet<FzOutputWheel> fzWheelRuleSet;

    FuzzyValueSet fzInputValueSet = new FuzzyValueSet();

    // These are used for debugging (see ApplyFuzzyRules() call
    // in Update()
    FuzzyValueSet mergedThrottle = new FuzzyValueSet();
    FuzzyValueSet mergedWheel = new FuzzyValueSet();


    private FuzzySet<FzInputDirection> GetDirectionSet()
    {
      FuzzySet<FzInputDirection> set = new FuzzySet<FzInputDirection>();
      set.Set(FzInputDirection.Left, new ShoulderMembershipFunction(30f, new Coords(30f, 1f), new Coords(6f, 0f), -30f));
      set.Set(FzInputDirection.Straight, new TriangularMembershipFunction(new Coords(30f, 0f), new Coords(0f, 1f), new Coords(-30f, 0f)));
      set.Set(FzInputDirection.Right, new ShoulderMembershipFunction(30f, new Coords(-6f, 0f), new Coords(-30f, 1f), -30f));
      return set;
    }

    private FuzzySet<FzInputSpeed> GetSpeedSet()
    {
      FuzzySet<FzInputSpeed> speedSet = new FuzzySet<FzInputSpeed>();

      speedSet.Set(FzInputSpeed.Slow, new ShoulderMembershipFunction(0f, new Coords(0f, 1f), new Coords(35f, 0f), 80f));
      speedSet.Set(FzInputSpeed.Medium, new TriangularMembershipFunction(new Coords(35f, 0f), new Coords(50f, 1f), new Coords(80f, 0f)));
      speedSet.Set(FzInputSpeed.Fast, new ShoulderMembershipFunction(0f, new Coords(50f, 0f), new Coords(80f, 1f), 80f));

      return speedSet;
    }

    private FuzzySet<FzOutputThrottle> GetThrottleSet()
    {
      FuzzySet<FzOutputThrottle> throttleSet = new FuzzySet<FzOutputThrottle>();

      throttleSet.Set(FzOutputThrottle.Brake, new ShoulderMembershipFunction(-80f, new Coords(-30f, 1), new Coords(-10f, 0f), 80f));
      throttleSet.Set(FzOutputThrottle.Coast, new TriangularMembershipFunction(new Coords(-80f, 0f), new Coords(0f, 1f), new Coords(80f, 0f)));
      throttleSet.Set(FzOutputThrottle.Accelerate, new ShoulderMembershipFunction(-80f, new Coords(50f, 0f), new Coords(80f, 1f), 80f));

      return throttleSet;
    }


    // FuzzySet for Wheel
    private FuzzySet<FzOutputWheel> GetWheelSet()
    {
      FuzzySet<FzOutputWheel> set = new FuzzySet<FzOutputWheel>();
      set.Set(FzOutputWheel.TurnLeft, new ShoulderMembershipFunction(-0.8f, new Coords(-0.8f, 1f), new Coords(-0.2f, 0), 0.8f));
      set.Set(FzOutputWheel.Straight, new TriangularMembershipFunction(new Coords(-0.8f, 0f), new Coords(0f, 1f), new Coords(0.8f, 0f)));
      set.Set(FzOutputWheel.TurnRight, new ShoulderMembershipFunction(0.8f, new Coords(0.2f, 0), new Coords(0.8f, 1), 0.8f));
      return set;
    }


    private FuzzyRule<FzOutputThrottle>[] GetThrottleRules()
    {

      FuzzyRule<FzOutputThrottle>[] rules =
      {
                // TODO: Add some rules. Here is an example
                // (Note: these aren't necessarily good rules)
                If(FzInputSpeed.Slow).Then(FzOutputThrottle.Accelerate),
                If(FzInputSpeed.Medium).Then(FzOutputThrottle.Coast),
                If(FzInputSpeed.Fast).Then(FzOutputThrottle.Brake),
                // More example syntax
                //If(And(FzInputSpeed.Fast, Not(FzFoo.Bar)).Then(FzOutputThrottle.Accelerate),
            };

      return rules;
    }

    private FuzzyRule<FzOutputWheel>[] GetWheelRules()
    {
      FuzzyRule<FzOutputWheel>[] rules =
      {
        // If the direction is left, then turn left
        If(FzInputDirection.Left).Then(FzOutputWheel.TurnRight),

        // If the direction is straight, then go straight
        If(FzInputDirection.Straight).Then(FzOutputWheel.Straight),

        // If the direction is right, then turn right
        If(FzInputDirection.Right).Then(FzOutputWheel.TurnLeft)
    };

      return rules;
    }

    private FuzzyRuleSet<FzOutputThrottle> GetThrottleRuleSet(FuzzySet<FzOutputThrottle> throttle)
    {
      var rules = this.GetThrottleRules();
      return new FuzzyRuleSet<FzOutputThrottle>(throttle, rules);
    }

    private FuzzyRuleSet<FzOutputWheel> GetWheelRuleSet(FuzzySet<FzOutputWheel> wheel)
    {
      var rules = this.GetWheelRules();
      return new FuzzyRuleSet<FzOutputWheel>(wheel, rules);
    }


    protected override void Awake()
    {
      base.Awake();

      StudentName = "Amir Bolous";

      // Only the AI can control. No humans allowed!
      IsPlayer = false;

    }

    protected override void Start()
    {
      base.Start();

      // TODO: You can initialize a bunch of Fuzzy stuff here
      fzSpeedSet = this.GetSpeedSet();
      fzDirectionSet = this.GetDirectionSet();

      fzThrottleSet = this.GetThrottleSet();
      fzThrottleRuleSet = this.GetThrottleRuleSet(fzThrottleSet);

      fzWheelSet = this.GetWheelSet();
      fzWheelRuleSet = this.GetWheelRuleSet(fzWheelSet);
    }

    System.Text.StringBuilder strBldr = new System.Text.StringBuilder();

    override protected void Update()
    {

      // TODO Do all your Fuzzy stuff here and then
      // pass your fuzzy rule sets to ApplyFuzzyRules()

      // Remove these once you get your fuzzy rules working.
      // You can leave one hardcoded while you work on the other.
      // Both steering and throttle must be implemented with variable
      // control and not fixed/hardcoded!

      // HardCodeSteering(0.2f);
      // HardCodeThrottle(0.2f);

      // Simple example of fuzzification of vehicle state
      // The Speed is fuzzified and stored in fzInputValueSet
      //TODO: remove
      Debug.Log("Speed: " + Speed);
      fzSpeedSet.Evaluate(Speed, fzInputValueSet);

      float direction = Vector3.SignedAngle(transform.forward, pathTracker.closestPointDirectionOnPath, Vector3.up);
      fzDirectionSet.Evaluate(direction, fzInputValueSet);

      // ApplyFuzzyRules evaluates your rules and assigns Thottle and Steering accordingly
      // Also, some intermediate values are passed back for debugging purposes
      // Throttle = someValue; //[-1f, 1f] -1 is full brake, 0 is neutral, 1 is full throttle
      // Steering = someValue; // [-1f, 1f] -1 if full left, 0 is neutral, 1 is full right

      ApplyFuzzyRules<FzOutputThrottle, FzOutputWheel>(
          fzThrottleRuleSet,
          fzWheelRuleSet,
          fzInputValueSet,
          // access to intermediate state for debugging
          out var throttleRuleOutput,
          out var wheelRuleOutput,
          ref mergedThrottle,
          ref mergedWheel
          );


      // Use vizText for debugging output
      // You might also use Debug.DrawLine() to draw vectors on Scene view
      if (vizText != null)
      {
        strBldr.Clear();

        strBldr.AppendLine($"Demo Output");
        strBldr.AppendLine($"Comment out before submission");

        // You will probably want to selectively enable/disable printing
        // of certain fuzzy states or rules

        AIVehicle.DiagnosticPrintFuzzyValueSet<FzInputSpeed>(fzInputValueSet, strBldr);

        AIVehicle.DiagnosticPrintRuleSet<FzOutputThrottle>(fzThrottleRuleSet, throttleRuleOutput, strBldr);
        AIVehicle.DiagnosticPrintRuleSet<FzOutputWheel>(fzWheelRuleSet, wheelRuleOutput, strBldr);

        vizText.text = strBldr.ToString();
      }

      // recommend you keep the base Update call at the end, after all your FuzzyVehicle code so that
      // control inputs can be processed properly (e.g. Throttle, Steering)
      base.Update();
    }

  }
}