from flask import render_template, flash, redirect
from forms import LoginForm, RegistrationForm
from flask import *
import sqlite3

sqlite_file = 'database.db'
table_name = 'users'
username_column = 'username'
pword_column = 'password'

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
		print "hi"
		print username
		print password
		c.execute("SELECT username, password FROM users where username=? and password=?", (username, password))
		user = c.fetchone()
		return redirect('/home')
		# query = "SELECT password FROM users WHERE username = \'{}\'".format(request.form['username'].lower())
		# cursor = mysql.connection.cursor()
		# cursor.execute(query)
		# if cursor.rowcount == 0:
		# 	#username not found
		# 	error_username = True
		# else:
		# 	#verify password is correct
		# 	password = cursor.fetchall()
		# 	print password[0][0]
		# 	a = str(password[0][0]).split('$')
		# 	salt = str(a[1])
		# 	algorithm = str(a[0])
		# 	print a
		# 	print str(algorithm)
		# 	m = hashlib.new(algorithm)
		# 	m.update(salt + str(request.form['password']))
		# 	password_hash = m.hexdigest()
		# 	db = "$".join([algorithm,salt,password_hash])
		# 	print db
		# 	print password[0][0]
		# 	if db == password[0][0]:
		# 		session['username'] = request.form['username']
		# 		if request.args.get('url'):
		# 			return redirect(request.args.get('url'))
		# 		else:
		# 			return redirect(url_for('main.main_route'))
		# 	else:
		# 		error_password = True
	return render_template('home.html')

@login.route('/register', methods = ['GET', 'POST'])
def register_route():
	if request.method == "GET":
		return render_template('register.html')
	elif request.method == "POST":
		username = request.form['username']
		password = request.form['password']
		c.execute("INSERT INTO users (username, password) values (?,?)",(username,password))
		conn.commit()
		return redirect('/home')

	
@login.route('/home', methods = ['GET','POST'])
def home_route():
	return render_template('index.html')

