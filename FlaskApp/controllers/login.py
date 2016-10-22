from flask import render_template, flash, redirect
from forms import LoginForm, RegistrationForm
from flask import *
from mysql import mysql

login = Blueprint('login', __name__)

@login.route('/login', methods = ['GET', 'POST'])
def login_route():
	form = LoginForm()

	if request.method == 'POST':
		query = "SELECT password FROM users WHERE username = \'{}\'".format(request.form['username'].lower())
		cursor = mysql.connection.cursor()
		cursor.execute(query)
		if cursor.rowcount == 0:
			#username not found
			error_username = True
		else:
			#verify password is correct
			password = cursor.fetchall()
			print password[0][0]
			a = str(password[0][0]).split('$')
			salt = str(a[1])
			algorithm = str(a[0])
			print a
			print str(algorithm)
			m = hashlib.new(algorithm)
			m.update(salt + str(request.form['password']))
			password_hash = m.hexdigest()
			db = "$".join([algorithm,salt,password_hash])
			print db
			print password[0][0]
			if db == password[0][0]:
				session['username'] = request.form['username']
				if request.args.get('url'):
					return redirect(request.args.get('url'))
				else:
					return redirect(url_for('main.main_route'))
			else:
				error_password = True
	return render_template('login.html', title='Sign In', form=form)

@login.route('/register', methods = ['GET', 'POST'])
def register_route():
	if request.method == "GET":
		print "fhsudhfsj"
		form = RegistrationForm()
		return render_template('register.html', title='Sign Up', form=form)
	elif request.method == "POST":
		print "POST"

		query = "INSERT into users (" + username + ", " + password + ")"
		cursor = mysql.connection.cursor()
		cursor.execute(query)
	
@login.route('/home', methods = ['GET','POST'])
def home_route():
	return render_template('index.html')

@login.route('/test', methods = ['POST'])
def registertest_route():
	if request.method == "POST":
		print "fsdfs"
		if form.validate_on_submit():
			return redirect('/index')
