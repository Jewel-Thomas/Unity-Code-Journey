// Unity Design Pattern Example: Factory
// This script demonstrates the Factory pattern in Unity
// Generated automatically - ready to use in your Unity project

Here's an example of the Factory design pattern implemented in C# for Unity:

```csharp
using UnityEngine;

// Abstract Product class
public abstract class Vehicle {
    public abstract void Drive();
}

// Concrete Products
public class Car : Vehicle {
    public override void Drive() {
        Debug.Log("Driving a car...");
    }
}

public class Truck : Vehicle {
    public override void Drive() {
        Debug.Log("Driving a truck...");
    }
}

// Abstract Factory interface
public abstract class VehicleFactory {
    public abstract Vehicle CreateVehicle(string vehicleType);
}

// Concrete Factories
public class CarFactory : VehicleFactory {
    public override Vehicle CreateVehicle(string vehicleType) {
        if (vehicleType == "car")
            return new Car();
        else
            return null;
    }
}

public class TruckFactory : VehicleFactory {
    public override Vehicle CreateVehicle(string vehicleType) {
        if (vehicleType == "truck")
            return new Truck();
        else
            return null;
    }
}

// Client code that uses the Factory pattern
public class VehicleManager : MonoBehaviour {
    private VehicleFactory factory;

    void Start() {
        // Create a car factory and order a car
        factory = new CarFactory();
        Vehicle car = factory.CreateVehicle("car");
        car.Drive();

        // Create a truck factory and order a truck
        factory = new TruckFactory();
        Vehicle truck = factory.CreateVehicle("truck");
        truck.Drive();
    }
}

```

This example demonstrates the Factory pattern in Unity development. It creates an abstract `Vehicle` class with concrete implementations like `Car` and `Truck`. The `VehicleFactory` interface defines a method to create vehicles, which is implemented by concrete factories like `CarFactory` and `TruckFactory`.

The `VehicleManager` script uses these factories to order and drive different types of vehicles. This example showcases how the Factory pattern helps create objects without specifying their exact classes, making it easier to add or remove vehicle types in the future.

To use this code in a Unity project:

1. Create a new C# script (e.g., `VehicleFactoryExample.cs`) and paste the above code into it.
2. Save the script in your Unity project's Assets folder.
3. Attach the `VehicleManager` script to an empty GameObject in your scene.
4. Run the scene to see the output of the vehicles being driven.

This example is practical and educational for Unity developers learning design patterns, as it demonstrates a real-world use case and provides clear comments explaining how the Factory pattern works.