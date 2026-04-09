
SECURITY_PROFILES = {

"strict": {
    "block": ["email","phone_number","ip","credit_card"],
    "allow": ["datetime","number"]
},

"standard": {
    "block": ["email","phone_number"],
    "allow": ["datetime","number","unit"]
},

"financial": {
    "block": ["credit_card","ssn","account_number","email"],
    "allow": ["datetime"]
},

"healthcare": {
    "block": ["patient_id","ssn","email","phone_number"],
    "allow": ["datetime","unit"]
}

}
