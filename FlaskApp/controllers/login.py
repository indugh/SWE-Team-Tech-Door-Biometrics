from flask import render_template, flash, redirect
from forms import LoginForm, RegistrationForm
from flask import *

login = Blueprint('login', __name__)

@login.route('/login', methods = ['GET', 'POST'])
def login_route():
	form = LoginForm()
	return render_template('login.html', title='Sign In', form=form)

@login.route('/register', methods = ['GET', 'POST'])
def register_route():
	if request.method == "GET":
		print "fhsudhfsj"
		form = RegistrationForm()
	return render_template('register.html', title='Sign Up', form=form)

@login.route('/home', methods = ['GET','POST'])
def home_route():
	return render_template('index.html')

@login.route('/test', methods = ['POST'])
def registertest_route():
	if request.method == "POST":
		print "fsdfs"
		if form.validate_on_submit():
			return redirect('/index')
