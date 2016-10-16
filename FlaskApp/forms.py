from flask_wtf import Form

from wtforms.fields import BooleanField, StringField, TextField, TextAreaField, PasswordField, SubmitField
from wtforms import validators

class LoginForm(Form):
	username = StringField('Username', [validators.Length(min=4, max=35)])
	password = PasswordField('New Password', [validators.InputRequired()])

class RegistrationForm(Form):
    username = StringField('Username', [validators.Length(min=4, max=25)])
    email = StringField('Email Address', [validators.Length(min=6, max=35)])
    password = PasswordField('New Password', [
        validators.DataRequired(),
        validators.EqualTo('confirm', message='Passwords must match')
    ])
    confirm = PasswordField('Repeat Password')
   ## remember_me = BooleanField('I accept the TOS', [validators.DataRequired()])