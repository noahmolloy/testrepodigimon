using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//Unlikely needs to be changed

public class PokemonParty : MonoBehaviour
{
    [SerializeField] List<Pokemon> pokemons;

    public List<Pokemon> Pokemons
    {
        get
        {
            return pokemons;
        }
    }

    private void Start()
    {
        foreach (var pokemon in pokemons)
        {
            pokemon.Init();
        }
    }

    public Pokemon GetHealthyPokemon()
    {
        return pokemons.Where(x => x.HP > 0).FirstOrDefault();
    }

    public void AddPokemon(Pokemon newPokemon)
    {
        if(pokemons.Count < 6)
        {
            pokemons.Add(newPokemon);
        }
        else
        {
            //TODO add to PC once implemented
        }
    }
}

//Reference: Pokemon in Unity Series on Youtube (Game Dev Experiments)
