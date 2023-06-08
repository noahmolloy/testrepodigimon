using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Unlikely needs change

public class MapArea : MonoBehaviour
{
    [SerializeField] List<Pokemon> wildPokemons;

    public Pokemon GetRandomWildPokemon() //generates a random wild pokemon from the list in unity
    {
        var wildPokemon = wildPokemons[Random.Range(0, wildPokemons.Count)];
        wildPokemon.Init();
        return wildPokemon;
    }
}

//Reference: Pokemon in Unity Series on Youtube (Game Dev Experiments)