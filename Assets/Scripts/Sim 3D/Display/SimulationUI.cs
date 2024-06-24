using UnityEngine;
using UnityEngine.UI;

public class SimulationUI : MonoBehaviour
{
    public Simulation3D simulation;
    public ParticleDisplay3D ParticleDisplay;
    public Spawner3D spawner;

    public Slider timeScaleSlider;
    public InputField velocityDisplayDif;
    public InputField massInput;
    public Slider collisionDampingSlider;
    public InputField smoothingRadiusInput;
    public InputField targetDensityInput;
    public InputField pressureMultiplierInput;
    public InputField nearPressureMultiplierInput;
    public InputField viscosityStrengthInput;

    // New UI elements for the Spawner3D
    public Slider numParticlesPerAxisSlider;
    public InputField numPointsInput;
    public InputField centreInput;
    public Slider sizeSlider;
    public InputField initialVelInput;
    public Slider jitterStrengthSlider;
    public InputField debug_numParticles;
    void Start()
    {
        // Set initial values for simulation parameters
        timeScaleSlider.value = simulation.timeScale;
        velocityDisplayDif.text = ParticleDisplay.velocityDisplayDif.ToString();
        massInput.text = simulation.mass.ToString();
        collisionDampingSlider.value = simulation.collisionDamping;
        smoothingRadiusInput.text = simulation.smoothingRadius.ToString();
        targetDensityInput.text = simulation.targetDensity.ToString();
        pressureMultiplierInput.text = simulation.pressureMultiplier.ToString();
        nearPressureMultiplierInput.text = simulation.nearPressureMultiplier.ToString();
        viscosityStrengthInput.text = simulation.viscosityStrength.ToString();

        // Set initial values for spawner parameters
        numParticlesPerAxisSlider.value = spawner.numParticlesPerAxis;
        numPointsInput.text = spawner.numPoints.ToString();
        centreInput.text = spawner.centre.x.ToString();
        sizeSlider.value = spawner.size;
        initialVelInput.text = spawner.initialVel.x.ToString();
        jitterStrengthSlider.value = spawner.jitterStrength;
        debug_numParticles.text = spawner.debug_numParticles.ToString();

        // Add listeners for simulation parameters
        timeScaleSlider.onValueChanged.AddListener(OnTimeScaleChanged);
        velocityDisplayDif.onEndEdit.AddListener(OnVelocityDisplayDifChanged);
        massInput.onEndEdit.AddListener(OnMassChanged);
        collisionDampingSlider.onValueChanged.AddListener(OnCollisionDampingChanged);
        smoothingRadiusInput.onEndEdit.AddListener(OnSmoothingRadiusChanged);
        targetDensityInput.onEndEdit.AddListener(OnTargetDensityChanged);
        pressureMultiplierInput.onEndEdit.AddListener(OnPressureMultiplierChanged);
        nearPressureMultiplierInput.onEndEdit.AddListener(OnNearPressureMultiplierChanged);
        viscosityStrengthInput.onEndEdit.AddListener(OnViscosityStrengthChanged);

        // Add listeners for spawner parameters
        numParticlesPerAxisSlider.onValueChanged.AddListener(OnNumParticlesPerAxisChanged);
        numPointsInput.onEndEdit.AddListener(OnNumPointsChanged);
        centreInput.onEndEdit.AddListener(OnCentreChanged);
        sizeSlider.onValueChanged.AddListener(OnSizeChanged);
        initialVelInput.onEndEdit.AddListener(OnInitialVelChanged);
        jitterStrengthSlider.onValueChanged.AddListener(OnJitterStrengthChanged);
    }

    void OnTimeScaleChanged(float value)
    {
        simulation.timeScale = value;
    }

    void OnVelocityDisplayDifChanged(string value)
    {
        if (int.TryParse(value, out int result))
        {
            ParticleDisplay.velocityDisplayDif = result;
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

    void OnNumParticlesPerAxisChanged(float value)
    {
        spawner.numParticlesPerAxis = (int)value;
    }

    void OnNumPointsChanged(string value)
    {
        if (int.TryParse(value, out int result))
        {
            spawner.numPoints = result;
        }
    }

    void OnCentreChanged(string value)
    {
        if (float.TryParse(value, out float result))
        {
            spawner.centre.x = result;
        }
    }

    void OnSizeChanged(float value)
    {
        spawner.size = value;
    }

    void OnInitialVelChanged(string value)
    {
        if (float.TryParse(value, out float result))
        {
            spawner.initialVel.x = -result;
        }
    }

    void OnJitterStrengthChanged(float value)
    {
        spawner.jitterStrength = value;
    }

    void Update(){
        debug_numParticles.text = spawner.debug_numParticles.ToString();
    }

}