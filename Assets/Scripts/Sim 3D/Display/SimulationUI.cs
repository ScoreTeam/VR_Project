using UnityEngine;
using UnityEngine.UI;

public class SimulationUI : MonoBehaviour
{
    public Simulation3D simulation;

    public Slider timeScaleSlider;
    public InputField iterationsPerFrameInput;
    public InputField massInput;
    public Slider collisionDampingSlider;
    public InputField smoothingRadiusInput;
    public InputField targetDensityInput;
    public InputField pressureMultiplierInput;
    public InputField nearPressureMultiplierInput;
    public InputField viscosityStrengthInput;

    void Start()
    {
        // Set initial values
        timeScaleSlider.value = simulation.timeScale;
        iterationsPerFrameInput.text = simulation.iterationsPerFrame.ToString();
        massInput.text = simulation.mass.ToString();
        collisionDampingSlider.value = simulation.collisionDamping;
        smoothingRadiusInput.text = simulation.smoothingRadius.ToString();
        targetDensityInput.text = simulation.targetDensity.ToString();
        pressureMultiplierInput.text = simulation.pressureMultiplier.ToString();
        nearPressureMultiplierInput.text = simulation.nearPressureMultiplier.ToString();
        viscosityStrengthInput.text = simulation.viscosityStrength.ToString();

        // Add listeners
        timeScaleSlider.onValueChanged.AddListener(OnTimeScaleChanged);
        iterationsPerFrameInput.onEndEdit.AddListener(OnIterationsPerFrameChanged);
        massInput.onEndEdit.AddListener(OnMassChanged);
        collisionDampingSlider.onValueChanged.AddListener(OnCollisionDampingChanged);
        smoothingRadiusInput.onEndEdit.AddListener(OnSmoothingRadiusChanged);
        targetDensityInput.onEndEdit.AddListener(OnTargetDensityChanged);
        pressureMultiplierInput.onEndEdit.AddListener(OnPressureMultiplierChanged);
        nearPressureMultiplierInput.onEndEdit.AddListener(OnNearPressureMultiplierChanged);
        viscosityStrengthInput.onEndEdit.AddListener(OnViscosityStrengthChanged);
    }

    void OnTimeScaleChanged(float value)
    {
        simulation.timeScale = value;
    }

    void OnFixedTimeStepChanged(bool value)
    {
        simulation.fixedTimeStep = value;
    }

    void OnIterationsPerFrameChanged(string value)
    {
        if (int.TryParse(value, out int result))
        {
            simulation.iterationsPerFrame = result;
        }
    }

    void OnMassChanged(string value)
    {
        if (float.TryParse(value, out float result))
        {
            simulation.mass = result;
        }
    }

    void OnCollisionDampingChanged(float value)
    {
        simulation.collisionDamping = value;
    }

    void OnSmoothingRadiusChanged(string value)
    {
        if (float.TryParse(value, out float result))
        {
            simulation.smoothingRadius = result;
        }
    }

    void OnTargetDensityChanged(string value)
    {
        if (float.TryParse(value, out float result))
        {
            simulation.targetDensity = result;
        }
    }

    void OnPressureMultiplierChanged(string value)
    {
        if (float.TryParse(value, out float result))
        {
            simulation.pressureMultiplier = result;
        }
    }

    void OnNearPressureMultiplierChanged(string value)
    {
        if (float.TryParse(value, out float result))
        {
            simulation.nearPressureMultiplier = result;
        }
    }

    void OnViscosityStrengthChanged(string value)
    {
        if (float.TryParse(value, out float result))
        {
            simulation.viscosityStrength = result;
        }
    }
}
