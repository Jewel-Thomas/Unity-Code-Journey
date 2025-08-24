// Unity Design Pattern Example: Observer
// This script demonstrates the Observer pattern in Unity
// Generated automatically - ready to use in your Unity project

Here is an example of the Observer pattern implemented in C# using Unity:

```C#
// Using statements
using UnityEngine;

// Subject interface defines the methods that will be called when its state changes.
public interface ISubject
{
    void Attach(IObserver observer);
    void Detach(IObserver observer);
    void Notify();
}

// Concrete subject class. It maintains a list of observers and notifies them whenever its state changes.
public class WeatherStation : ISubject
{
    private List<IObserver> _observers;
    private float _temperature;
    private float _humidity;

    public WeatherStation()
    {
        _observers = new List<IObserver>();
    }

    public void Attach(IObserver observer)
    {
        _observers.Add(observer);
    }

    public void Detach(IObserver observer)
    {
        _observers.Remove(observer);
    }

    public void Notify()
    {
        foreach (var observer in _observers)
        {
            observer.Update(_temperature, _humidity);
        }
    }

    public void MeasureTemperature(float temperature)
    {
        _temperature = temperature;
        Notify();
    }

    public void MeasureHumidity(float humidity)
    {
        _humidity = humidity;
        Notify();
    }
}

// Observer interface defines the method that will be called when the subject's state changes.
public interface IObserver
{
    void Update(float temperature, float humidity);
}

// Concrete observer class. It displays the weather data and can also trigger other actions based on it.
public class WeatherDisplay : MonoBehaviour, IObserver
{
    private WeatherStation _weatherStation;

    public void Start()
    {
        // Attach to the subject in Start() method so that we're guaranteed to be attached before any measurements are taken.
        _weatherStation = new WeatherStation();
        _weatherStation.Attach(this);
    }

    public void Update(float temperature, float humidity)
    {
        Debug.Log("Temperature: " + temperature + ", Humidity: " + humidity);

        // Trigger other actions based on the weather data
        if (temperature > 20)
        {
            Debug.Log("It's hot!");
        }
        else if (humidity > 0.5f)
        {
            Debug.Log("It's humid!");
        }
    }

    public void OnDestroy()
    {
        _weatherStation.Detach(this);
    }
}

// Usage example:
public class Program
{
    public static void Main(string[] args)
    {
        // Create a weather station and display the weather data
        WeatherDisplay display = new WeatherDisplay();

        // Measure temperature and humidity
        float temperature = 25.0f;
        float humidity = 0.4f;

        display.MeasureTemperature(temperature);
        display.MeasureHumidity(humidity);

        // Output:
        // Temperature: 25, Humidity: 0.4
        // It's hot!
    }
}
```

In this example, we have a `WeatherStation` that maintains a list of observers and notifies them whenever its state changes (temperature or humidity). We have a `WeatherDisplay` that is an observer for the weather station and displays the temperature and humidity. The `WeatherDisplay` also triggers other actions based on the weather data.

This example demonstrates how to use the Observer pattern in Unity, where the `WeatherStation` is the subject and the `WeatherDisplay` is the observer.