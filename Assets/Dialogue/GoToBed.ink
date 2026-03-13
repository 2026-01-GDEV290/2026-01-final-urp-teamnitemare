-> endnight

===endnight===
Do you want to go to sleep?
    + [Yes, I'm tired...]
    -> sleep ("SLEEP")
    + [Not yet.]
    -> sleep ("Stay awake.")
    ===sleep(answer)===
    You chose to {answer}
    -> END