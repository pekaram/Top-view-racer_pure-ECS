﻿using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;
using System.Collections.Generic;
using System;

public class SystemManager : MonoBehaviour 
{
    /// <summary>
    /// Reference to the hero prefab along with its children.
    /// </summary>
    [SerializeField]
    private CarHirerachyIndex heroCarHirerachyIndex;
  
    /// <summary>
    /// Street cars that act as obstacles to avoid, currently they are only 1 type.
    /// </summary>
    [SerializeField]
    private GameObject streetCarPrefab;

    /// <summary>
    /// A generation place on asphalt 
    /// </summary>
    [SerializeField]
    private GameObject startSlotPrefab;

    /// <summary>
    /// The car speed UI
    /// </summary>
    [SerializeField]
    private Text speedText;

    /// <summary>
    /// The street gameobjecgt
    /// </summary>
    [SerializeField]
    private GameObject street;
    
    /// <summary>
    /// Reference to world's entity manager.
    /// </summary>
    private EntityManager entityManager;

    /// <summary>
    /// Hero entity reference, used to easily pick it.
    /// </summary>
    private Entity hero;

    /// <summary>
    /// Hero id used to indentify hero's car
    /// </summary>
    private Guid heroId = Guid.NewGuid();
    
    /// <summary>
    /// Hero's car's box collider size.
    /// </summary>
    private Vector3 heroBoxColliderSize;

    /// <summary>
    /// Street car's box collider size.
    /// </summary>
    private Vector3 streetCarBoxColliderSize;

    /// <summary>
    /// The size of <see cref="streetCarPrefab"/>'s collider.
    /// </summary>
    private CapsuleColliderData streetCarCapsuleData;

    /// <summary>
    /// All street cars excluding hero's car
    /// </summary>
    private List<Entity> streetCars = new List<Entity>();

    public static int numberOfGenerationSlots = 5;

    private void Awake()
    {
        this.heroBoxColliderSize = this.GetBoxColliderSize(this.heroCarHirerachyIndex.gameObject);
        this.streetCarBoxColliderSize = this.GetBoxColliderSize(this.streetCarPrefab);
        this.streetCarCapsuleData = this.GetCapusleSize(this.streetCarPrefab);
    }

    private void Start()
    {
        entityManager = World.Active.EntityManager;
        this.CreateStreetCars();
        this.CreateStartingSlots();
        this.CreateHeroCar();
    }

    private Vector3 GetBoxColliderSize(GameObject targetPrefab)
    {
        var modelObject = Instantiate(targetPrefab);
        var collider = modelObject.GetComponent<BoxCollider>();
        if (collider == null)
        {
            Debug.LogError("No box collider was found attached to this object");
        }

        var size = collider.bounds.size;
        Destroy(modelObject);
        return size;
    }

    private CapsuleColliderData GetCapusleSize(GameObject targetPrefab)
    {
        var modelObject = Instantiate(targetPrefab);
        var collider = modelObject.GetComponent<CapsuleCollider>();
        if (collider == null)
        {
            Debug.LogError("No capsule collider was found attached to this object");
        }

        Destroy(modelObject);
        return new CapsuleColliderData { Height = collider.height, Radius = collider.radius };
    }

    private void CreateStartingSlots()
    {
        for (var i = 0; i <= Settings.NumberOfGenerationSlots; i++)
        {
            var entity = this.CreateEntityFromPrefab(this.startSlotPrefab);
            var creationposition = this.entityManager.GetComponentData<Translation>(entity).Value;
            var shiftedPosition = new Translation { Value = new float3(creationposition.x - (i * 2), creationposition.y, creationposition.z) };
            this.entityManager.SetComponentData<Translation>(entity, shiftedPosition);
            this.entityManager.AddComponentData(entity, new GenerationSlotComponent { Position = shiftedPosition });
        }
    }

    /// <summary>
    /// Creates the hero car.
    /// </summary>
    private void CreateHeroCar()
    {
        var carReferences = Instantiate(this.heroCarHirerachyIndex);
        this.hero = this.CreateCarStructure(carReferences);
        this.AddHeroCompnents();
    }

    /// <summary>
    /// Creates a car entity and maintains its children hirerachy in DOTS while seperating children them into seperate entities  
    /// </summary>
    /// <param name="carHirerachyIndex"> to rip the data from </param>
    /// <returns> the parent car </returns>
    private Entity CreateCarStructure(CarHirerachyIndex carHirerachyIndex)
    {
        // Create a DOTS clone with the wheels removed, first seperate them into seperate entities and clone them to stick tags on them.
        carHirerachyIndex.SwitchWheels(false);
        var carEntity =  this.CreateEntityFromGameObject(carHirerachyIndex.ParentCar, false); 
        var wheels = carHirerachyIndex.GetAllWheels();
        foreach (var wheel in wheels)
        {
            var wheelEntity = this.CreateEntityFromGameObject(wheel);
            this.entityManager.AddComponentData(wheelEntity, new WheelComponent { Parent = carEntity });
            this.entityManager.AddComponentData(wheelEntity, new Parent { Value = carEntity });
            this.entityManager.AddComponentData(wheelEntity, new LocalToParent { });
            var buffer = this.entityManager.AddBuffer<LinkedEntityGroup>(carEntity);
            buffer.Add(wheelEntity);
            
            Destroy(wheel);
        }
        
        // Destroy parent's clone after ripping all children out and only leave the duplicate DOTS entity with its children in scene.
        Destroy(carHirerachyIndex.ParentCar);

        return carEntity;
    }

    /// <summary>
    /// Assigns components for the hero car <see cref="hero"/>
    /// </summary>
    private void AddHeroCompnents()
    {
        this.entityManager.AddComponentData(this.hero, new HeroComponent());
        var carPosition = this.entityManager.GetComponentData<Translation>(this.hero);
        carPosition.Value.z -= 2;
        carPosition.Value.y -= 0.2f;
        this.entityManager.SetComponentData<Translation>(this.hero, carPosition);
        this.entityManager.AddComponentData(this.hero, new CarComponent() { ID = this.heroId, CubeColliderSize = this.heroBoxColliderSize});        
    }

    /// <summary>
    /// Creates entity from prefabs
    /// </summary>
    /// <param name="source"> to create from </param>
    /// <returns> entity</returns>
    private Entity CreateEntityFromPrefab(GameObject source)
    {
        var convertedGameObject = GameObjectConversionUtility.ConvertGameObjectHierarchy(source, World.Active);
        return entityManager.Instantiate(convertedGameObject);
    }
    
    /// <summary>
    /// Creates a duplicate ECS entity from a gameobject
    /// </summary>
    /// <param name="source"> to clone </param>
    /// <param name="destroySource"> will destroy the original GameObject and leave the DOTS entity only </param>
    /// <returns> the created entity </returns>
    private Entity CreateEntityFromGameObject(GameObject source, bool destroySource = true)
    {
        if(this.IsGameObjectActive(source))
        {
            Debug.LogError("Use CreateEntityFromPrefab instead");
        }

        var convertedGameObject = GameObjectConversionUtility.ConvertGameObjectHierarchy(source, World.Active);
        Destroy(source);
        return convertedGameObject;
    }

    /// <summary>
    /// A check for making sure objects are active before creating their DOTS entity
    /// </summary>
    /// <returns> true if active or could be activated </returns>
    private bool IsGameObjectActive(GameObject target)
    {
        Debug.LogWarning("Target GameObject found in-active before cloning, trying to activate");
        target.SetActive(true);
        // SetActive won't have any effect on prefabs, unlike scene objects
        // Any gameobject needs to be active before being passed to DOTS.
        return !target.activeInHierarchy;
    }

    private void CreateStreetCars()
    {
        for(var i = 0; i < 2; i++)
        {
            var carEntity = this.CreateEntityFromPrefab(this.streetCarPrefab);
            var carPosition = this.entityManager.GetComponentData<Translation>(carEntity);
            this.streetCars.Add(carEntity);
            carPosition.Value.x -= 4f * i;
            carPosition.Value.z += 1;
            this.entityManager.SetComponentData<Translation>(carEntity, carPosition);
            this.entityManager.AddComponentData(carEntity, new CarComponent { ID = Guid.NewGuid(), Speed = 20, CubeColliderSize = this.streetCarBoxColliderSize, CapsuleColliderData = this.streetCarCapsuleData });
        }
    }

    /// <summary>
    /// A generic add component
    /// </summary>
    private void AddComponentGeneric(Entity entity, IComponentData component)
    {
        MethodInfo methodInfo = typeof(EntityManager).GetMethod("AddComponentData");
        MethodInfo genericMethodInfo = methodInfo.MakeGenericMethod(component.GetType());
        var parameters = new object[] {entity, component};
        object componentData = genericMethodInfo.Invoke(this.entityManager, parameters);
    }

    private void FixedUpdate()
    {
        var data = this.entityManager.GetComponentData<CarComponent>(hero);
        speedText.text = Mathf.RoundToInt(data.Speed).ToString();
        this.UpdateStreet();
    }

    /// <summary>
    /// Polling for game end
    /// </summary>
    private void Update()
    {
        var didEnd = this.entityManager.GetComponentData<CarComponent>(this.hero).IsCollided;   
        if (!didEnd)
        {
            return;
        }

        this.enabled = false;

        foreach(var system in this.entityManager.World.Systems)
        {
            system.Enabled = false;
        }
    }

    /// <summary>
    /// Scrolls the street for moving world
    /// </summary>
    private void UpdateStreet()
    {
        var data = this.entityManager.GetComponentData<CarComponent>(hero);
        speedText.text = Mathf.RoundToInt(data.Speed).ToString();

        if (street.transform.position.z > -12)
        {
            street.transform.Translate(0, 0, -data.Speed/100);
        }
        else
        {
            street.transform.position = new Vector3(0, 0, 0);
        }
    }
}
