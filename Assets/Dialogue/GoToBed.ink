INCLUDE globals.ink

-> endnight

===endnight===
0
Do you want to go to sleep?
    + [Yes, I'm tired...]
    -> sleep ("SLEEP")
    + [Not yet.]
    -> sleep ("Stay awake.")
    ===sleep(answer)===
    ~ bed = bed
    You chose to {answer}
    -> END