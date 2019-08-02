from flask import render_template, flash, redirect
from forms import LoginForm, RegistrationForm
from flask import *
import sqlite3

sqlite_file = 'database.db'
table_name = 'users'
username_column = 'username'
pword_column = 'password'


ikey = DUO_IKEY
skey = DUO_SKEY
akey = DUO_AKEY

from duo_web import sign_request, verify_response

conn = sqlite3.connect(sqlite_file, check_same_thread=False)
c = conn.cursor()

login = Blueprint('login', __name__)

@login.route('/login', methods = ['GET', 'POST'])
def login_route():
	if request.method == 'GET':
		return render_template('login.html')
	if request.method == 'POST':
		username = request.form['username']
		password = request.form['password']
		if not username or not password:
			flash('Please Enter Both Fields')
			return render_template('login.html')
		print username
		print password
		c.execute("SELECT * from users")
		print c.fetchall()
		c.execute("SELECT username, password FROM users where username=? and password=?", (username, password))
		user = c.fetchone()
		print user
		if user:
			sig_request = sign_request(ikey, skey, akey, username)
			return render_template('duo_login.html', sig_request = sig_request, post_action = request.path)
		else:
			flash('Inavlid Password')
			return render_template('login.html')

@login.route('/duo_login', methods = ['GET' ,'POST'])
def duo_login_route():
	return render_template('duo_login.html')

@login.route('/register', methods = ['GET', 'POST'])
def register_route():
	if request.method == "GET":
		return render_template('register.html')
	elif request.method == "POST":
		username = request.form['username']
		password = request.form['password']
		if not username or not password:
			flash('Please enter both fields')
			return render_template('register.html')
		else:
			#check if username is in db
			user = c.execute('SELECT * FROM users WHERE username=?', (username,)).fetchall()
			print user
			if user:
				flash("The username already exists")
				return render_template('register.html')
			sig_request = sign_request('DIFRGMA77D2LMZYAESSU', '3PaauXz74LrY9l7aEVxdrbeP8IryyNhToMBemr3I', akey, username)
			c.execute('INSERT INTO users (username, password) values (?,?)',(username,password))
			conn.commit()
		return render_template('duo_login.html', sig_request = sig_request, post_action = request.path)
	
@login.route('/home', methods = ['GET','POST'])
def home_route():
	if request.method == "POST":
		c.execute("INSERT INTO registered (registered) values(1)")
		session['username'] = username
		sig_response = request.args.get("sig_response") 
		authenticated_username = verify_response(ikey, skey, akey, sig_response)
		if authenticated_username:
  			log_user_in(authenticated_username)
	return render_template('index.html')


